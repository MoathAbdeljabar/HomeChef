
using HomeChef.Data;
using HomeChef.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HomeChef.Application.Auth.Services;

public partial class AuthService
{



    public bool IsOver18(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - date.Year;

        // Subtract one year if birthday hasn't occurred yet this year
        if (date > today.AddYears(-age))
        {
            age--;
        }

        return age >= 18;
    }
    public async Task<string> _GenerateJwtTokenAsync(ApplicationUser user)
    {


        // Ensure user has a security stamp (should always exist)
        if (string.IsNullOrEmpty(user.SecurityStamp))
        {
            // This shouldn't happen, but just in case
            await _userManager.UpdateSecurityStampAsync(user);
        }

        var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        // ===== ADDED: Security Stamp Claim =====
        // This allows us to validate the security stamp on every request
        // When the stamp changes (logout/password change), all existing tokens become invalid
        new Claim("AspNet.Identity.SecurityStamp", user.SecurityStamp)
    };

        // Get roles for the user
        var roles = await _userManager.GetRolesAsync(user);

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:SecretKey"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expireMinutes = Convert.ToDouble(_configuration["Jwt:ExpireMinutes"]);
        var expires = DateTime.Now.AddMinutes(expireMinutes);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        /*
         GetPrincipalFromExpiredToken() - Reads the EXPIRED JWT token and extracts user information from it (even though it's expired)
           principal - Contains the decoded user information from the token:
           -- User ID
           -- Username
           -- Roles
           -- Other claims

         */

        if (string.IsNullOrEmpty(token) || token.Split('.').Length != 3)
        // token is malformed/invalid - it doesn't have the proper 3-part JWT structure (header.payload.signature)
        {
            throw new SecurityTokenException("Invalid token format - must have 3 parts");
        }

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidAudience = _configuration["Jwt:Audience"],
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"])),
            ValidateLifetime = false // Important: we want to read expired tokens
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
        //The signature validation happens automatically here

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }

    public double GetAccessTokenExpiryMinutes()
    {
        return Convert.ToDouble(_configuration["Jwt:ExpireMinutes"] ?? "15");
    }


}

