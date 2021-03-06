﻿using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Vecto.Application.Profile;
using Vecto.Core.Interfaces;

namespace Vecto.Api.Controllers
{
    [ApiConventionType(typeof(DefaultApiConventions))]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProfileController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IValidator<ProfileDTO> _profileValidator;

        public ProfileController(
            IUserRepository userRepository,
            UserManager<IdentityUser> userManager,
            IValidator<ProfileDTO> profileValidator
        )
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _profileValidator = profileValidator;
        }

        [HttpGet("")]
        public IActionResult Get()
        {
            string email = User.Identity.Name;
            if (email is null) return Unauthorized();

            var user = _userRepository.GetBy(email);
            if (user is null) return BadRequest();

            return Ok(user.MapToDTO());
        }

        [HttpDelete("")]
        public async Task<IActionResult> Delete()
        {
            string email = User.Identity.Name;
            if (email is null) return Unauthorized();

            var user = _userRepository.GetBy(email);
            var identityUser = await _userManager.FindByNameAsync(email);
            if (user is null || identityUser is null) return BadRequest();

            // Delete User
            _userRepository.Delete(user);
            _userRepository.SaveChanges();

            // Delete IdentityUser
            var result = await _userManager.DeleteAsync(identityUser);
            if (!result.Succeeded) return BadRequest();

            return Ok();
        }

        [HttpPatch("")]
        public async Task<IActionResult> Update(ProfileDTO profileDTO)
        {
            string email = User.Identity.Name;
            if (email is null) return Unauthorized();

            var validation = await _profileValidator.ValidateAsync(profileDTO);
            if (!validation.IsValid) return BadRequest(validation);

            var user = _userRepository.GetBy(email);
            var identityUser = await _userManager.FindByNameAsync(email);
            if (user is null || identityUser is null) return BadRequest();

            // Update User
            user.UpdateWith(profileDTO);
            _userRepository.Update(user);
            _userRepository.SaveChanges();

            // Update IdentityUser
            identityUser.UpdateWith(profileDTO);
            var result = await _userManager.UpdateAsync(identityUser);
            if (!result.Succeeded) return BadRequest();

            return Ok(user.MapToDTO());
        }
    }
}
