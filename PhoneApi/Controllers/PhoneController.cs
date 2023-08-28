using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhoneApi.Models;
using PhoneBookHumanGroupBL.InterfacesOfManagers;
using PhoneBookHumanGroupDL.ContextInfo;
using PhoneBookHumanGroupEL.Entities;
using PhoneBookHumanGroupEL.ViewModels;

namespace PhoneApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhoneController : ControllerBase
    {
        private readonly IMemberPhoneManager _memberPhoneManager;
        private readonly PhoneBookHumanGroupContext _phoneBookHumanGroupContext;
        protected readonly IMapper _mapper;

        public PhoneController(PhoneBookHumanGroupContext phoneBookHumanGroupContext, IMapper mapper, IMemberPhoneManager memberPhoneManager)
        {
            _phoneBookHumanGroupContext = phoneBookHumanGroupContext;
            _mapper = mapper;
            _memberPhoneManager = memberPhoneManager;
        }



        //public PhoneController(IMemberPhoneManager memberPhoneManager, PhoneBookHumanGroupContext phoneBookHumanGroupContext)
        //{
        //    //_memberPhoneManager = memberPhoneManager;
        //    _phoneBookHumanGroupContext = phoneBookHumanGroupContext;
        //}

        [HttpDelete]
        public APIResponse<MemberPhoneVM> PhoneDelete(int id)
        {
            try
            {
                var phoneMember = _memberPhoneManager.GetbyId(id).Data;
                var phone = _memberPhoneManager.Delete(phoneMember);

                if (phone.IsSuccess)
                    return new APIResponse<MemberPhoneVM>()
                    {
                        IsSuccess = true,
                        Message = phone.Message,
                        Data = new MemberPhoneVM()
                    };
                else
                    return new APIResponse<MemberPhoneVM>()
                    {
                        IsSuccess = false,
                        Message = phone.Message,
                        Data = phoneMember
                    };
                var phoneMember2 = _phoneBookHumanGroupContext.MemberPhoneTable.Where(x => x.Id == id).FirstOrDefault();
                var phoneMember2VM = _mapper.Map<MemberPhone, MemberPhoneVM>(phoneMember2);
                var phone2 = _phoneBookHumanGroupContext.MemberPhoneTable.Remove(phoneMember2);
                var phone2VM = _mapper.Map<MemberPhone, MemberPhoneVM>(phoneMember2);
                var result = _phoneBookHumanGroupContext.SaveChanges();

                if (result > 0)
                    return new APIResponse<MemberPhoneVM>()
                    {
                        IsSuccess = true,
                        Message = "Silme işlemi başarılı",
                        Data = new MemberPhoneVM()
                    };
                else
                    return new APIResponse<MemberPhoneVM>()
                    {
                        IsSuccess = false,
                        Message = "Silinecek kayıt bulunamadı",
                        Data = phoneMember2VM
                    };

            }
            catch (Exception ex)
            {
                return new APIResponse<MemberPhoneVM>()
                {
                    IsSuccess = false,
                    Message = $"Beklenmedik bir hata oldu! {ex.Message}",
                    Data = new MemberPhoneVM()
                };
            }
        }

    }
}
