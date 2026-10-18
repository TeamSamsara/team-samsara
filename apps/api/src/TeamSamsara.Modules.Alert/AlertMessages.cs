// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Alert/AlertMessages.cs
// Version : 2.0.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : Subject and body of every alert type, built on the shared email layout, with {{token}} placeholders filled from the caller's template data.

using TeamSamsara.Shared.Alert;

namespace TeamSamsara.Modules.Alert;

internal static class AlertMessages
{
    #region Fields

    private const string RecoveryDays = "{{" + AlertTemplateKeys.RecoveryDays + "}}";
    private const string NewEmail = "{{" + AlertTemplateKeys.NewEmail + "}}";
    private const string IpAddress = "{{" + AlertTemplateKeys.IpAddress + "}}";
    private const string UserAgent = "{{" + AlertTemplateKeys.UserAgent + "}}";

    public const string RegistrationCodeSubject = "Your Team Samsara confirmation code";
    public static readonly string RegistrationCodeBody = AlertEmailLayout.Render(
        "Confirm your email",
        "Welcome to Team Samsara. Enter this code to finish creating your account.",
        "If you did not create an account, you can ignore this email. Nothing happens until the code is used.",
        showCode: true);

    public const string LoginCodeSubject = "Your Team Samsara sign-in code";
    public static readonly string LoginCodeBody = AlertEmailLayout.Render(
        "Sign in on a new device",
        "We do not recognise the device you are signing in from. Enter this code to continue.",
        "If this was not you, someone knows your password. Reset it as soon as you can.",
        showCode: true);

    public const string PasswordChangeCodeSubject = "Confirm your password change";
    public static readonly string PasswordChangeCodeBody = AlertEmailLayout.Render(
        "Confirm your password change",
        "Enter this code to confirm the new password you chose.",
        "If you did not ask for this, do not enter the code. Your password stays as it is.",
        showCode: true);

    public const string PasswordResetCodeSubject = "Reset your Team Samsara password";
    public static readonly string PasswordResetCodeBody = AlertEmailLayout.Render(
        "Reset your password",
        "Enter this code to choose a new password for your account.",
        "If you did not ask to reset your password, you can ignore this email.",
        showCode: true);

    public const string EmailChangeCurrentCodeSubject = "Confirm your email change";
    public static readonly string EmailChangeCurrentCodeBody = AlertEmailLayout.Render(
        "Confirm your email change",
        "A request was made to change the sign-in email on your account. Enter this code to confirm it is you.",
        "If you did not ask for this, do not enter the code and change your password.",
        showCode: true,
        details: new[] { ("Requested new address", NewEmail) });

    public const string EmailChangeNewCodeSubject = "Confirm your new email address";
    public static readonly string EmailChangeNewCodeBody = AlertEmailLayout.Render(
        "Confirm your new address",
        "Enter this code to confirm that this address will be your new sign-in email.",
        "If you did not expect this, you can ignore this email. The address is not used until the code is entered.",
        showCode: true);

    public const string AccountDeletionCodeSubject = "Confirm account deletion";
    public static readonly string AccountDeletionCodeBody = AlertEmailLayout.Render(
        "Confirm account deletion",
        "Enter this code to delete your account. You can still restore it by signing in within " + RecoveryDays + " days.",
        "If you did not ask to delete your account, do not enter the code and change your password.",
        showCode: true);

    public const string StepUpCodeSubject = "Confirm it is you";
    public static readonly string StepUpCodeBody = AlertEmailLayout.Render(
        "Confirm it is you",
        "Enter this code to confirm your request.",
        "If you did not ask for this, do not enter the code.",
        showCode: true);

    public const string NewLoginAttemptSubject = "New sign-in attempt on your account";
    public static readonly string NewLoginAttemptBody = AlertEmailLayout.Render(
        "New sign-in attempt",
        "Someone tried to sign in to your account from a device we do not know. We asked for a confirmation code first.",
        "If this was you, there is nothing to do. If not, reset your password now.",
        details: new[] { ("IP address", IpAddress), ("Device", UserAgent) });

    public const string PasswordResetRequestedSubject = "A password reset was requested";
    public static readonly string PasswordResetRequestedBody = AlertEmailLayout.Render(
        "Password reset requested",
        "A password reset was requested for your account.",
        "If this was you, use the code we sent separately. If not, you can ignore it; your password has not changed.",
        details: new[] { ("IP address", IpAddress), ("Device", UserAgent) });

    public const string PasswordChangedSubject = "Your password was changed";
    public static readonly string PasswordChangedBody = AlertEmailLayout.Render(
        "Your password was changed",
        "The password on your Team Samsara account was just changed, and every session was signed out.",
        "If this was not you, reset your password right away and contact us.",
        details: new[] { ("What happened", "Password changed"), ("Sessions", "Signed out everywhere") });

    public const string EmailChangedSubject = "Your sign-in email was changed";
    public static readonly string EmailChangedBody = AlertEmailLayout.Render(
        "Your sign-in email was changed",
        "The sign-in email on your account was changed. This address no longer signs in to Team Samsara.",
        "If this was not you, contact us immediately.",
        details: new[] { ("New address", NewEmail), ("Sessions", "Signed out everywhere") });

    public const string AccountDeletedSubject = "Your account was deleted";
    public static readonly string AccountDeletedBody = AlertEmailLayout.Render(
        "Your account was deleted",
        "Your Team Samsara account was deleted. It can still be restored.",
        "After " + RecoveryDays + " days the account and its data are permanently removed.",
        details: new[] { ("Restore until", RecoveryDays + " days from now"), ("How", "Sign in with your email and password") });

    #endregion
}
