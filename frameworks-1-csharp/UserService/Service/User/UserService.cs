using UserEntity = UserService.Domain.User;
using UserService.Repository.User;

namespace UserService.Service.User;

public class UserService
{
    private readonly UserRepository _repository;

    public UserService(UserRepository repository)
    {
        _repository = repository;
    }

    public void CreateUser(UserEntity user)
    {
        if (string.IsNullOrEmpty(user.Name))
        {
            throw new ArgumentException("name cannot be empty");
        }

        if (string.IsNullOrEmpty(user.Email))
        {
            throw new ArgumentException("email cannot be empty");
        }

        if (user.Password.Length < 8)
        {
            throw new ArgumentException("password must be at least 8 characters long");
        }

        if (user.Age <= 0)
        {
            throw new ArgumentException("age must be greater than 0");
        }

        _repository.CreateUser(user);
    }

    public UserEntity GetUserById(int id)
    {
        var user = _repository.GetUserById(id);
        if (user is null)
        {
            throw new KeyNotFoundException("user not found");
        }

        return user;
    }

    public IReadOnlyList<UserEntity> GetUsers()
    {
        return _repository.GetUsers();
    }
}
