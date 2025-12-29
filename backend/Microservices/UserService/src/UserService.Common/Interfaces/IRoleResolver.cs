using System;
using UserService.src.UserService.Repository.EfCore.Entities;

namespace UserService.src.UserService.Common.Interfaces;
    public interface IRoleResolver
{
        Task<List<Role>> ResolveRolesAsync(IEnumerable<string> roleNames, Guid userId);
}
