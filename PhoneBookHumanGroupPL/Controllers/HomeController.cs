using MessagePack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PhoneApi.Models;
using PhoneBookHumanGroupBL.EmailSenderManager;
using PhoneBookHumanGroupBL.IEmailSender;
using PhoneBookHumanGroupBL.InterfacesOfManagers;
using PhoneBookHumanGroupEL.ViewModels;
using PhoneBookHumanGroupPL.Models;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace PhoneBookHumanGroupPL.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMemberManager _memberManager;
        private readonly IMemberPhoneManager _memberPhoneManager;
        private readonly IPhoneGroupManager _phoneGroupManager;
        private readonly IEmailSender _emailSender;

        public HomeController(ILogger<HomeController> logger, IMemberManager memberManager, IMemberPhoneManager memberPhoneManager, IPhoneGroupManager phoneGroupManager, IEmailSender emailSender)
        {
            _logger = logger;
            _memberManager = memberManager;
            _memberPhoneManager = memberPhoneManager;
            _phoneGroupManager = phoneGroupManager;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize] //login olmadan bu sayfaya erişmesini istemiyoruz
        [HttpGet]
        public IActionResult Phones()
        {
            try
            {
                //kim giriş yaptı ise ona ait telefon rehberini sayfaya göndereceğiz
                var loggedInUserEmail = HttpContext.User.Identity?.Name;

                //email adresinden member tablosundan useri bulacağız böylece idsini memberphone tablosundki FK için kullanabiliriz
                var user = _memberManager.GetByCondition(x => x.Email.ToLower() == loggedInUserEmail.ToLower()).Data;

                //select * from MemberPhone where MemberId=idno
                var phones = _memberPhoneManager.GetAll(x => x.MemberId == user.Id).Data;

                if (phones == null)
                {
                    ViewBag.PhonesPageMsg = $"Rehberinizde kayıtlı kişi henüz yoktur!";
                    return View(new List<MemberPhoneVM>());
                }
                else
                {
                    return View(phones);
                }

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Beklenmedik bir hata oluştu!");
                //ex loglanacak
                return View();
            }
        }

        [Authorize]
        public IActionResult PhoneAdd()
        {
            try
            {
                // Müşterinin istediği tasarıma göre
                //Sayfaya phonegrouplar gitmelidir
                var phoneGroups = _phoneGroupManager.GetAll(x => x.IsActive).Data;
                if (phoneGroups.Count == 0 || phoneGroups == null)
                {
                    ViewBag.PhoneGroups = new List<PhoneGroupVM>();
                    ViewBag.DefaultPhoneGroupId = 0;
                }
                else
                {
                    ViewBag.PhoneGroups = phoneGroups;

                    ViewBag.DefaultPhoneGroupId = phoneGroups.FirstOrDefault().Id;
                }

                return View(new MemberPhoneVM());
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Beklenmedik bir hata oluştu!");
                ViewBag.PhoneGroups = new List<PhoneGroupVM>();
                //ex loglanacak
                return View();
            }
        }



        [HttpPost]
        [Authorize]
        public IActionResult PhoneAdd(MemberPhoneVM model)
        {
            try
            {
                var phoneGroups = _phoneGroupManager.GetAll(x => x.IsActive).Data;
                if (phoneGroups == null)
                    ViewBag.PhoneGroups = new List<PhoneGroupVM>();

                else
                    ViewBag.PhoneGroups = phoneGroups;


                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                //İşlemi yapan user
                //kim giriş yaptı ise ona ait telefon rehberini sayfaya göndereceğiz
                var loggedInUserEmail = HttpContext.User.Identity?.Name;

                //email adresinden member tablosundan useri bulacağız böylece idsini memberphone tablosundki FK için kullanabiliriz
                var user = _memberManager.GetByCondition(x => x.Email.ToLower() == loggedInUserEmail.ToLower()).Data;

                //Eğer PhoneGRoupId=0 gelirse.....
                if (model.PhoneGroupId == 0)
                {

                    var phoneGrp = new PhoneGroupVM()
                    {
                        CreatedDate = DateTime.Now,
                        Name = model.AnotherPhoneGroupName,
                        IsActive = true
                    };

                    var isSamephoneGrp = _phoneGroupManager.GetByCondition(x => x.Name.ToLower() == model.AnotherPhoneGroupName.ToLower() && x.IsActive).Data;

                    if (isSamephoneGrp == null)
                    {
                        var result = _phoneGroupManager.Add(phoneGrp).Data;
                        model.PhoneGroupId = result.Id;
                    }
                    else
                    {
                        model.PhoneGroupId = isSamephoneGrp.Id;
                    }

                }
                model.MemberId = user.Id;
                model.CreatedDate = DateTime.Now;
                model.PhoneNumber = $"{model.CountryPhoneCode} {model.Phone}";

                //Aynı telefondan zaten var mı?

                if (_memberPhoneManager.GetAll(x => x.PhoneNumber == model.PhoneNumber).Data.Count > 0)
                {
                    ModelState.AddModelError("", $"{model.PhoneNumber} rehberde zaten eklidir!");
                    return View(model);

                }



                if (_memberPhoneManager.Add(model).IsSuccess)
                {
                    TempData["PhoneAddSuccessMsg"] = $"{model.PhoneGroupNameSurname} Rehber Eklendi! ";

                    #region MailGonderelim
                    EmailMessageModel m = new EmailMessageModel()
                    {
                        To = user.Email,
                        Subject = $"Human Group Telefon Rehberi - Rehbere Yeni Kişi Eklendi!",
                        Body = $"<p>Merhaba {user.Name} {user.Surname}, </p> <br/> <p> Rehberinize {model.PhoneGroupNameSurname} adlı kişiyi eklediniz. </p>"
                    };

                    _emailSender.SendEmail(m);

                    #endregion

                    return RedirectToAction("Phones", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "REhbere Kişi eklenemedi! Tekrar deneyiniz!");
                    return View(model);
                }

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Beklenmedik bir hata oluştu!");
                return View(model);

            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult PhoneEdit(int id)
        {
            try
            {
                var phoneGroups = _phoneGroupManager.GetAll(x => x.IsActive).Data;
                if (phoneGroups.Count == 0 || phoneGroups == null)
                {
                    ViewBag.PhoneGroups = new List<PhoneGroupVM>();
                    ViewBag.DefaultPhoneGroupId = 0;
                }
                else
                {
                    ViewBag.PhoneGroups = phoneGroups;

                    ViewBag.DefaultPhoneGroupId = phoneGroups.FirstOrDefault().Id;
                }
                var memberPhone = _memberPhoneManager.GetbyId(id).Data;
                memberPhone.Phone = memberPhone.PhoneNumber.Split(" ")[1];
                return View(memberPhone);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Beklenmedik bir hata oluştu!");
                ViewBag.PhoneGroups = new List<PhoneGroupVM>();
                //ex loglanacak
                return View();
            }
        }

        [HttpPost]
        [Authorize]
        public IActionResult PhoneEdit(MemberPhoneVM model)
        {
            try
            {
                var phoneGroups = _phoneGroupManager.GetAll(x => x.IsActive).Data;
                if (phoneGroups == null)
                    ViewBag.PhoneGroups = new List<PhoneGroupVM>();

                else
                    ViewBag.PhoneGroups = phoneGroups;


                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var loggedInUserEmail = HttpContext.User.Identity?.Name;

                var user = _memberManager.GetByCondition(x => x.Email.ToLower() == loggedInUserEmail.ToLower()).Data;

                if (model.PhoneGroupId == 0)
                {

                    var phoneGrp = new PhoneGroupVM()
                    {
                        CreatedDate = DateTime.Now,
                        Name = model.AnotherPhoneGroupName,
                        IsActive = true
                    };

                    var isSamephoneGrp = _phoneGroupManager.GetByCondition(x => x.Name.ToLower() == model.AnotherPhoneGroupName.ToLower() && x.IsActive).Data;

                    if (isSamephoneGrp == null)
                    {
                        var result = _phoneGroupManager.Add(phoneGrp).Data;
                        model.PhoneGroupId = result.Id;
                    }
                    else
                    {
                        model.PhoneGroupId = isSamephoneGrp.Id;
                    }

                }
                model.MemberId = user.Id;
                model.CreatedDate = DateTime.Now;
                model.PhoneNumber = $"{model.CountryPhoneCode} {model.Phone}";

                //Aynı telefondan zaten var mı?

                if (_memberPhoneManager.GetAll(x => x.PhoneNumber == model.PhoneNumber && x.Id != model.Id).Data.Count > 0)
                {
                    ModelState.AddModelError("", $"{model.PhoneNumber} rehberde zaten eklidir!");
                    return View(model);

                }



                if (_memberPhoneManager.Update(model).IsSuccess)
                {
                    TempData["PhoneAddSuccessMsg"] = $"{model.PhoneGroupNameSurname} Rehber Güncellendi! ";

                    #region MailGonderelim
                    EmailMessageModel m = new EmailMessageModel()
                    {
                        To = user.Email,
                        Subject = $"Human Group Telefon Rehberi - Rehberdeki kişi güncellendi!",
                        Body = $"<p>Merhaba {user.Name} {user.Surname}, </p> <br/> <p> Rehberinizdeki {model.PhoneGroupNameSurname} adlı kişiyi güncellediniz. </p>"
                    };

                    _emailSender.SendEmail(m);

                    #endregion

                    return RedirectToAction("Phones", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Rehberdeki Kişi Güncellenemedi! Tekrar deneyiniz!");
                    return View(model);
                }

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Beklenmedik bir hata oluştu!");
                return View(model);

            }
        }



        public IActionResult PhoneDelete(int id)
        {
            try
            {


                var response = new APIResponse<MemberPhoneVM>();
                using (WebClient client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;

                    #region delete
                    string url = $"https://localhost:7281/api/Phone?id={id}";
                    var yanit = client.UploadString(url,"DELETE","");
                    #endregion

                    #region delete  çıktısı
                     response = JsonConvert.DeserializeObject<APIResponse<MemberPhoneVM>>(yanit);
                    #endregion

                }
                ViewBag.response=response;
                var loggedInUserEmail = HttpContext.User.Identity?.Name;

                //email adresinden member tablosundan useri bulacağız böylece idsini memberphone tablosundki FK için kullanabiliriz
                var user = _memberManager.GetByCondition(x => x.Email.ToLower() == loggedInUserEmail.ToLower()).Data;

                //select * from MemberPhone where MemberId=idno
                var phones = _memberPhoneManager.GetAll(x => x.MemberId == user.Id).Data;

                if (phones == null)
                {
                    ViewBag.PhonesPageMsg = $"Rehberinizde kayıtlı kişi henüz yoktur!";
                    return RedirectToAction("Phones", new List<MemberPhoneVM>());
                }
                else
                {
                    return RedirectToAction("Phones", phones);

                    return View(phones);
                }

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Beklenmedik bir hata oluştu!");
                return RedirectToAction("Phones");

            }
        }





    }
}