using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;
using System.IO;
using UserService.src.UserService.Common.Interfaces;
using UserService.src.UserService.Repository.EfCore.Entities;

namespace UserService.src.UserService.Host.Features.Roles;

public class RoleResolver : IRoleResolver
{
    private readonly IUserRepository _userRepository;

    public RoleResolver(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<Role>> ResolveRolesAsync(IEnumerable<string> roleNames, Guid userId)
    {
        var allRoles = await _userRepository.GetUserRoles();
        if (allRoles == null)
        {
            //log error of getting all roles
            return new List<Role>();
        }

        //if(roleNames == null || !roleNames.Any())
        //{
        //    //log error of getting string roles from request
        //    return new List<Role>();
        //}

        var user = await _userRepository.GetUserByIdAsync(userId);

        if (user == null)
        {
            //log error of getting user by id
            return new List<Role>();
        }


        var resolvedRoles = allRoles.Where(r => roleNames.Contains(r.Name, StringComparer.OrdinalIgnoreCase)).ToList();

        if (resolvedRoles != null)
            if(resolvedRoles.Count != roleNames.Count())
            {
             //log error of some roles not found
            }

        foreach (var role in resolvedRoles)
        {
            if (!user.Roles.Any(ur => ur.Role.Name.Equals(role.Name, StringComparison.OrdinalIgnoreCase)))
            {
                await _userRepository.AddRoleToUser(userId, role, new CancellationToken());
            }
        }
        foreach (var userRole in user.Roles.ToList())
        {
            if (!resolvedRoles.Any(r => r.Name.Equals(userRole.Role.Name, StringComparison.OrdinalIgnoreCase)))
            {
                await _userRepository.RemoveRoleFromUser(userId, userRole.Role, new CancellationToken());
            }
        }

        return resolvedRoles;
    }
}
