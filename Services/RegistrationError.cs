namespace CorpLinkBaseMinimal.Services;

public enum RegistrationError
{
    None,
    UsernameEmpty,
    UsernameTaken,
    EmailEmpty,
    EmailInvalid,
    EmailTaken,
    PasswordEmpty,
    PasswordTooWeak
}