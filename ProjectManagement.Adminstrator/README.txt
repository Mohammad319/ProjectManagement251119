ProjectManagement.Adminstrator - Stage 9

Focus of this patch set:
- Account/Manage area unification
- Shared account layout + side navigation + status messages
- Profile / Email / Change Password
- 2FA / Recovery Codes / Authenticator setup
- Passkeys management
- Personal Data page
- Expanded Tailwind component layer in wwwroot/app.tailwind.css

Files included:
- wwwroot/app.tailwind.css
- Components/Account/Shared/ManageLayout.razor
- Components/Account/Shared/ManageNavMenu.razor
- Components/Account/Shared/StatusMessage.razor
- Components/Account/Shared/ShowRecoveryCodes.razor
- Components/Account/Pages/Manage/Index.razor
- Components/Account/Pages/Manage/Email.razor
- Components/Account/Pages/Manage/ChangePassword.razor
- Components/Account/Pages/Manage/TwoFactorAuthentication.razor
- Components/Account/Pages/Manage/Disable2fa.razor
- Components/Account/Pages/Manage/EnableAuthenticator.razor
- Components/Account/Pages/Manage/GenerateRecoveryCodes.razor
- Components/Account/Pages/Manage/PersonalData.razor
- Components/Account/Pages/Manage/Passkeys.razor
- Components/Account/Pages/Manage/RenamePasskey.razor

Apply by copying these files into your project and then rebuilding Tailwind/CSS and the solution.
