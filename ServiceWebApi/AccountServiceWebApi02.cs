using Model;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ServiceWebApi
{
    public class AccountServiceWebApi02
    {
        private WebApiAccess _webApiAccess;

        public AccountServiceWebApi02(WebApiAccess webApiAccess)
        {
            _webApiAccess = webApiAccess;
        }

        public async Task CreateUser(UserInfo user)
        {
            try
            {
                WebApiPost<UserInfo> webApiPost = new WebApiPost<UserInfo>(_webApiAccess);
                await webApiPost.PostAsync("/api/Account/CreateUser", user);
                Console.WriteLine("CreateUser ok");
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        public async Task<UserInfo> GetUserInfo(string username)
        {
            try
            {
                WebApiGet<UserInfo> webApiGet = new WebApiGet<UserInfo>(_webApiAccess);
                UserInfo user = await webApiGet.GetAsync($"api/account/GetUserInfo/{username}");
                Console.WriteLine("GetUserInfo ok");
                return user;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task Update(UserInfo user)
        {
            try
            {
                WebApiPost<UserInfo> webApiPost = new WebApiPost<UserInfo>(_webApiAccess);
                await webApiPost.PostAsync("/api/Account/UpdateUser", user);
                Console.WriteLine("UpdateUser ok");
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task UserLock(string username)
        {
            try
            {
                WebApiGet webApiGet = new WebApiGet(_webApiAccess);
                await webApiGet.GetAsync($"/api/Account/UserLock/{username}");
                Console.WriteLine("UserLock ok");
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task UserUnLock(string username)
        {
            try
            {
                WebApiGet webApiGet = new WebApiGet(_webApiAccess);
                await webApiGet.GetAsync($"/api/Account/UserUnLock/{username}");
                Console.WriteLine("UserUnLock ok");
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task UserLockItSelf(string username, string password)
        {
            try
            {
                WebApiGet webApiGet = new WebApiGet(_webApiAccess);
                await webApiGet.GetAsync($"/api/Account/UserLockItSelf/{username}");
                Console.WriteLine("UserLockItSelf ok");
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


    }



}
