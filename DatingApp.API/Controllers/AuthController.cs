﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
    private readonly IAuthRepository _repo;
    private readonly IConfiguration _config;
    public AuthController(IAuthRepository repo, IConfiguration config)
    {
      _repo = repo;
      _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register (UserForRegisterDto userForRegisterDto)
    {
      userForRegisterDto.UserName = userForRegisterDto.UserName.ToLower();
      if(await _repo.UserExists(userForRegisterDto.UserName))
      {
        return BadRequest("Username already exists");
      }

      var userToCreate = new User
      {
        UserName = userForRegisterDto.UserName
      };

      var createdUser = await _repo.Register(userToCreate, userForRegisterDto.Password);

      return StatusCode(201);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
    {
      var userFromRepo = await _repo.Login(userForLoginDto.UserName.ToLower(), userForLoginDto.Password);

      if(userFromRepo == null)
      {
        return Unauthorized();
      }

      var claims = new[]
      {
        new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
        new Claim(ClaimTypes.Name, userFromRepo.UserName),

      };

      //Create signing key
      var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));
      var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);
      //Create token
      var tokenDescriptor = new SecurityTokenDescriptor
      {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.Now.AddDays(1),
        SigningCredentials = creds

      };
      //create token handler
      var tokenHandler = new JwtSecurityTokenHandler();
      var token = tokenHandler.CreateToken(tokenDescriptor);
      return Ok(new
      {
        token = tokenHandler.WriteToken(token)
      });
    }
  }
}