using UserEntity = UserService.Domain.User;

namespace UserService.Repository.User;

public class UserRepository
{
    private readonly Dictionary<int, UserEntity> _users = new();
    private int _nextId = 1;

    public void CreateUser(UserEntity user)
    {
        user.Id = _nextId;
        _nextId++;
        _users[user.Id] = user;
    }

    public UserEntity? GetUserById(int id)
    {
        return _users.GetValueOrDefault(id);
    }

    public IReadOnlyList<UserEntity> GetUsers()
    {
        return _users.Values.ToList();
    }
}
