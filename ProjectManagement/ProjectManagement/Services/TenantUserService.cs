using AuthPermissions.Context;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using ProjectManagement.Components.ControlComponents.Department;
using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Services
{
    public interface ITenantUserService
    {
        Task<bool> RecreateUserAsync(TenantUserDto tenantUser);
        Task<List<TenantUserDto>> GetAllTenantUsersAsync(int? department);
        Task<bool> UpdateUserAsync(TenantUserDto user);
        Task<bool> RegisterAsync(TenantUserDto request);
        Task<bool> RemoveAsync(string id, bool onlyfromregister, int userid);

    }
    public interface ICurrentTenantService
    {
        int TenantId { get; }
    }
    public class CurrentTenantService(IHttpContextAccessor _httpContextAccessor) : ICurrentTenantService
    {
        public int TenantId
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                var claimValue = user?.FindFirst(PMClaimsConst.Tentan)?.Value;

                if (!int.TryParse(claimValue, out var tenantId))
                    throw new UnauthorizedAccessException("TenantId is missing or invalid.");

                return tenantId;
            }
        }
    }

    public class TenantUserService(UserManager<ApplicationUser> _userManager, ShardingSingleDbContext _shContext, ICurrentTenantService _currentTenant) : ITenantUserService
    {
        #region Password Generator
        string GenerateRandomPassword(int length = 10)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?";
            var random = new Random();
            return new string(Enumerable.Repeat(validChars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        #endregion

        #region Auth Register User
        async Task<ApplicationUser?> AuthRegisterUserAsync(TenantUserDto tenantUser, string temporaryPassword)
        {
            if (tenantUser == null) return null;

            // تحقق من وجود المستخدم مسبقًا
            var existingUser = await _userManager.FindByEmailAsync(tenantUser.Email);
            if (existingUser != null) return existingUser;

            var newUser = new ApplicationUser
            {
                Email = tenantUser.Email,
                Firstname = tenantUser.Firstname,
                Lastname = tenantUser.Lastname,
                UserName = tenantUser.Email,
                TenantId = _currentTenant.TenantId,
                DepartmentId = tenantUser.DepartmentId,
                LockoutEnabled = tenantUser.LockoutEnabled,
                LockoutStart = tenantUser.LockoutStart,
                LockoutEnd = tenantUser.LockoutEnd,
                PhoneNumber = tenantUser.PhoneNumber,
                PhoneNumberConfirmed = tenantUser.PhoneNumberConfirmed,
                UserId = tenantUser.Id
            };

            var result = await _userManager.CreateAsync(newUser, temporaryPassword);
            if (!result.Succeeded) return null;

            return newUser;
        }
        #endregion

        #region Register Tenant User
        public async Task<bool> RegisterAsync(TenantUserDto request)
        {
            if (string.IsNullOrEmpty(request.Role)) request.Role = PMRolesConst.Tenant.Admin;

            string password = GenerateRandomPassword();
            var identityUser = await AuthRegisterUserAsync(request, password);
            if (identityUser == null) return false;

            try
            {
                await _userManager.AddToRoleAsync(identityUser, request.Role);

                // تحقق من وجود المستخدم المحلي مسبقًا
                var localUser = await _shContext.User.FirstOrDefaultAsync(x => x.Email == request.Email);
                if (localUser == null)
                {
                    localUser = new UserEntity
                    {
                        ExternalAuthId = identityUser.Id,
                        FirstName = request.Firstname,
                        LastName = request.Lastname,
                        Email = request.Email,
                        TenantId = _currentTenant.TenantId,
                        DepartmentId = request.DepartmentId,
                        UserName = request.Email,
                    };
                    _shContext.User.Add(localUser);
                    await _shContext.SaveChangesAsync();

                    identityUser.UserId = localUser.Id;
                    await _userManager.UpdateAsync(identityUser);
                }

                return true;
            }
            catch (Exception ex)
            {
                // في حال فشل التسجيل، حذف الحساب من AspNetUsers
                await _userManager.DeleteAsync(identityUser);
                Console.WriteLine($"Register failed: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Delete User From Auth
        public async Task<bool> DeleteUserFromAuthAsync(string authId)
        {
            var user = await _userManager.FindByIdAsync(authId);
            if (user == null || user.TenantId != _currentTenant.TenantId) return false;
            var result = await _userManager.DeleteAsync(user); return result.Succeeded;
        }
        public async Task<bool> RemoveAsync(string id, bool onlyfromregister, int userid)
        {
            bool deletFrumAuth = true;
            if (!string.IsNullOrEmpty(id))
                deletFrumAuth = await DeleteUserFromAuthAsync(id.ToString());
            var u = await _shContext.User.FirstOrDefaultAsync(x => x.ExternalAuthId == id || x.Id == userid);
            if (!onlyfromregister && u != null && deletFrumAuth)
            {
                var calcs = _shContext.Calculations.Where(x => x.IsPrivate && x.CreatedBy == u.Id);
                if (calcs != null) _shContext.Calculations.RemoveRange(calcs);
                if (u != null) _shContext.User.Remove(u);
                await _shContext.SaveChangesAsync();
            }
            return true;
        }
        #endregion

        #region Recreate User
        public async Task<bool> RecreateUserAsync(TenantUserDto tenantUser)
        {
            if (tenantUser == null) return false;
            if (string.IsNullOrEmpty(tenantUser.Role)) tenantUser.Role = PMRolesConst.Tenant.Admin;

            string password = GenerateRandomPassword();
            var authUser = await AuthRegisterUserAsync(tenantUser, password);
            if (authUser == null) return false;

            await _userManager.AddToRoleAsync(authUser, tenantUser.Role);

            // تحديث TenantUser
            var userEntity = await _shContext.User.FirstOrDefaultAsync(x => x.Id == tenantUser.Id);
            if (userEntity != null)
            {
                userEntity.ExternalAuthId = authUser.Id;
                _shContext.User.Update(userEntity);
                await _shContext.SaveChangesAsync();
            }

            return true;
        }
        #endregion

        #region Update User
        public async Task<bool> UpdateUserAsync(TenantUserDto user)
        {
            ApplicationUser? oldUser = null;
            if (!string.IsNullOrEmpty(user.IdAuth))
                oldUser = await _userManager.FindByIdAsync(user.IdAuth);

            if (oldUser != null && oldUser.TenantId == _currentTenant.TenantId)
            {
                oldUser.Firstname = user.Firstname;
                oldUser.Lastname = user.Lastname;
                oldUser.PhoneNumber = user.PhoneNumber;
                oldUser.PhoneNumberConfirmed = user.PhoneNumberConfirmed;
                oldUser.LockoutEnabled = user.LockoutEnabled;
                oldUser.LockoutStart = user.LockoutStart;
                oldUser.LockoutEnd = user.LockoutEnd;

                await _userManager.UpdateAsync(oldUser);
                await _userManager.UpdateSecurityStampAsync(oldUser);

                // تحديث TenantUser
                var userEntity = await _shContext.User.FirstOrDefaultAsync(x => x.Id == user.Id);
                if (userEntity != null)
                {
                    userEntity.DepartmentId = user.DepartmentId;
                    userEntity.FirstName = user.Firstname;
                    userEntity.LastName = user.Lastname;
                    userEntity.ExternalAuthId = user.IdAuth;
                    _shContext.User.Update(userEntity);
                    await _shContext.SaveChangesAsync();
                }

                // تحديث الدور إذا تغير
                var roles = await _userManager.GetRolesAsync(oldUser);
                if (!roles.Contains(user.Role))
                {
                    await _userManager.RemoveFromRolesAsync(oldUser, roles);
                    await _userManager.AddToRoleAsync(oldUser, user.Role);
                }

                return true;
            }

            return false;
        }
        #endregion

        #region Get All Tenant Users
        public async Task<List<TenantUserDto>> GetAllTenantUsersAsync(int? department = null)
        {
            var tenantUsersQuery = _shContext.User.AsQueryable();
            if (department.HasValue)
                tenantUsersQuery = tenantUsersQuery.Where(x => x.DepartmentId == department.Value);

            var tenantUsers = await tenantUsersQuery.ToListAsync();

            var authUsers = await _userManager.Users
                .Where(x => x.TenantId == _currentTenant.TenantId)
                .ToListAsync();

            var authDict = authUsers.ToDictionary(x => x.Id, x => x);

            return tenantUsers.Select(tUser =>
            {
                authDict.TryGetValue(tUser.ExternalAuthId ?? "", out var appUser);

                return new TenantUserDto
                {
                    Id = tUser.Id,
                    Firstname = tUser.FirstName,
                    Lastname = tUser.LastName,
                    IdAuth = tUser.ExternalAuthId,
                    DepartmentId = tUser.DepartmentId,
                    Username = tUser.UserName,
                    Email = tUser.Email,
                    IsInAuth = appUser != null,
                    LockoutEnabled = appUser?.LockoutEnabled ?? false,
                    LockoutEnd = appUser?.LockoutEnd,
                    LockoutStart = appUser?.LockoutStart
                };
            }).ToList();
        }
        #endregion
    }

}
