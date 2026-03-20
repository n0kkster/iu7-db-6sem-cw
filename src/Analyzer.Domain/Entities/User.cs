using Analyzer.Domain.Enums;

namespace Analyzer.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public Role Role { get; private set; }
    public Guid TeamId { get; private set; }

    public User(string username, string email, string passwordHash)
    {
        Id = Guid.NewGuid();
        Username = string.IsNullOrWhiteSpace(username)
                 ? throw new ArgumentException("Имя пользователя обязательно", nameof(username))
                 : username;

        Email = string.IsNullOrWhiteSpace(email)
              ? throw new ArgumentException("Email обязателен", nameof(email))
              : !IsValidEmail(email) 
              ? throw new ArgumentException("Email невалиден", nameof(email))
              : email;

        PasswordHash = string.IsNullOrWhiteSpace(passwordHash)
                     ? throw new ArgumentException("Хэш пароля обязателен", nameof(passwordHash))
                     : passwordHash;

        TeamId = Guid.Empty;
        Role = Role.Unauthorized;
    }

    public void UpdateProfile(string username, string email)
    {
        Username = string.IsNullOrWhiteSpace(username)
                 ? throw new ArgumentException("Имя пользователя обязательно", nameof(username))
                 : username;

        Email = string.IsNullOrWhiteSpace(email)
              ? throw new ArgumentException("Email обязателен", nameof(email))
              : !IsValidEmail(email) 
              ? throw new ArgumentException("Email невалиден", nameof(email))
              : email;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Новый хэш пароля не может быть пустым");

        PasswordHash = newPasswordHash;
    }

    internal void SetRole(Role newRole)
    {
        if (Role != Role.Unauthorized)
            throw new InvalidOperationException("Роль пользователя уже установлена");

        Role = newRole;
    }

    internal void AttachToTeam(Guid teamId)
    {
        if (TeamId != Guid.Empty)
            throw new InvalidOperationException("Пользователь уже находится в команде");

        TeamId = teamId;
    }

    private bool IsValidEmail(string email)
    {
        var trimmedEmail = email.Trim();

        if (trimmedEmail.EndsWith("."))
            return false; 
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == trimmedEmail;
        }
        catch
        {
            return false;
        }
    }
}