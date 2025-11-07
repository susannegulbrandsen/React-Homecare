using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using HomeCareApp.DTOs;
using HomeCareApp.Models;
using HomeCareApp.Repositories.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HomeCareApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase // ControllerBase is sufficient for API controllers
    {
        private readonly UserManager<AuthUser> _userManager;
        private readonly SignInManager<AuthUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly IPatientRepository _patientRepository;
        private readonly IEmployeeRepository _employeeRepository;

        public AuthController(UserManager<AuthUser> userManager, SignInManager<AuthUser> signInManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration, ILogger<AuthController> logger, IPatientRepository patientRepository, IEmployeeRepository employeeRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _logger = logger;
            _patientRepository = patientRepository;
            _employeeRepository = employeeRepository;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // Validate role
            if (registerDto.Role != "Patient" && registerDto.Role != "Employee")
            {
                return BadRequest(new[] { new { code = "InvalidRole", description = "Role must be either 'Patient' or 'Employee'" } });
            }

            var user = new AuthUser
            {
                UserName = registerDto.Username,
                Email = registerDto.Email
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            
            if (result.Succeeded)
            {
                // Ensure roles exist
                if (!await _roleManager.RoleExistsAsync("Patient"))
                    await _roleManager.CreateAsync(new IdentityRole("Patient"));
                if (!await _roleManager.RoleExistsAsync("Employee"))
                    await _roleManager.CreateAsync(new IdentityRole("Employee"));

                // Add user to role
                await _userManager.AddToRoleAsync(user, registerDto.Role);

                _logger.LogInformation("[AuthAPIController] user registered for {@username} with role {@role}", registerDto.Username, registerDto.Role);
                return Ok(new { Message = "User registered successfully. Please complete your profile." });
            }

            _logger.LogWarning("[AuthAPIController] user registration failed for {@username}", registerDto.Username);
            var errors = result.Errors.Select(e => new { code = e.Code, description = e.Description });
            return BadRequest(errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var user = await _userManager.FindByNameAsync(loginDto.Username);

            if (user != null && await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                _logger.LogInformation("[AuthAPIController] user authorised for {@username}", loginDto.Username);
                var token = await GenerateJwtToken(user);
                return Ok(new { Token = token });
            }
            
            _logger.LogWarning("[AuthAPIController] user not authorised for {@username}", loginDto.Username);
            return Unauthorized();
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // For token-based authentication, logout is typically handled on the client-side by clearing the token. This endpoint can be used for things like token invalidation if implemented.
            await _signInManager.SignOutAsync(); // This is more for cookie-based auth, but doesn't hurt.
            _logger.LogInformation("[AuthAPIController] user logged out");
            return Ok(new { Message = "Logout successful" });
        }

        [Authorize]
        [HttpPost("complete-patient-profile")]
        public async Task<IActionResult> CompletePatientProfile([FromBody] PatientProfileDto profileDto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return NotFound("User not found");
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Patient"))
            {
                return BadRequest("User is not a patient");
            }

            // Check if patient record already exists
            var patients = await _patientRepository.GetAll();
            var existingPatient = patients.FirstOrDefault(p => p.UserId == user.Id);
            
            if (existingPatient != null)
            {
                return BadRequest("Patient profile already exists");
            }

            var patient = new Patient
            {
                FullName = profileDto.FullName,
                Address = profileDto.Address,
                DateOfBirth = profileDto.DateOfBirth,
                phonenumber = profileDto.PhoneNumber,
                HealthRelated_info = profileDto.HealthRelatedInfo,
                UserId = user.Id,
                User = user,
                Appointments = new List<Appointment>()
            };

            await _patientRepository.Create(patient);
            _logger.LogInformation("[AuthAPIController] Patient profile created for {Username}", user.UserName);
            
            return Ok(new { Message = "Patient profile created successfully" });
        }

        [Authorize]
        [HttpPost("complete-employee-profile")]
        public async Task<IActionResult> CompleteEmployeeProfile([FromBody] EmployeeProfileDto profileDto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return NotFound("User not found");
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Employee"))
            {
                return BadRequest("User is not an employee");
            }

            // Check if employee record already exists
            var employees = await _employeeRepository.GetAll();
            var existingEmployee = employees.FirstOrDefault(e => e.UserId == user.Id);
            
            if (existingEmployee != null)
            {
                return BadRequest("Employee profile already exists");
            }

            var employee = new Employee
            {
                FullName = profileDto.FullName,
                Address = profileDto.Address,
                Department = profileDto.Department,
                UserId = user.Id,
                User = user,
                Appointments = new List<Appointment>()
            };

            await _employeeRepository.Create(employee);
            _logger.LogInformation("[AuthAPIController] Employee profile created for {Username}", user.UserName);
            
            return Ok(new { Message = "Employee profile created successfully" });
        }

        private async Task<AuthUser?> GetCurrentUserAsync()
        {
            // Get all NameIdentifier claims (there might be multiple)
            var allNameIdentifierClaims = User.FindAll(ClaimTypes.NameIdentifier).Select(c => c.Value).ToList();
            var userIdFromNameId = User.FindFirst("nameid")?.Value;
            var userIdFromSub = User.FindFirst("sub")?.Value;
            var userIdFromUserid = User.FindFirst("userid")?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? 
                          User.FindFirst("username")?.Value ??
                          User.Identity?.Name;
            
            AuthUser? user = null;
            
            // Try to find user by each possible ID (prioritize GUID format)
            var possibleIds = new List<string?> { userIdFromSub, userIdFromNameId, userIdFromUserid }
                .Concat(allNameIdentifierClaims)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();
                
            foreach (var id in possibleIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                
                // Check if it looks like a GUID (prioritize these)
                if (Guid.TryParse(id, out _))
                {
                    user = await _userManager.FindByIdAsync(id);
                    if (user != null) break;
                }
            }
            
            // If still not found, try by username
            if (user == null && !string.IsNullOrEmpty(username))
            {
                user = await _userManager.FindByNameAsync(username);
            }
            
            // Try non-GUID IDs as last resort
            if (user == null)
            {
                foreach (var id in possibleIds)
                {
                    if (string.IsNullOrEmpty(id) || Guid.TryParse(id, out _)) continue;
                    
                    user = await _userManager.FindByIdAsync(id);
                    if (user != null) break;
                }
            }
            
            return user;
        }

        [Authorize]
        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount()
        {
            // Get all NameIdentifier claims (there might be multiple)
            var allNameIdentifierClaims = User.FindAll(ClaimTypes.NameIdentifier).Select(c => c.Value).ToList();
            var userIdFromNameId = User.FindFirst("nameid")?.Value;
            var userIdFromSub = User.FindFirst("sub")?.Value;
            var userIdFromUserid = User.FindFirst("userid")?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? 
                          User.FindFirst("username")?.Value ??
                          User.Identity?.Name;
            
            // Log all available claims for debugging
            var allClaims = User.Claims.Select(c => $"{c.Type}={c.Value}").ToArray();
            _logger.LogInformation("[AuthAPIController] All Claims: {Claims}", string.Join(", ", allClaims));
            _logger.LogInformation("[AuthAPIController] All NameIdentifier claims: {NameIdentifiers}, nameid: {NameId}, sub: {Sub}, userid: {UserId}, username: {Username}", 
                string.Join(", ", allNameIdentifierClaims), userIdFromNameId, userIdFromSub, userIdFromUserid, username);
            
            AuthUser? user = null;
            string? actualUserId = null;
            
            // Try to find user by each possible ID (prioritize GUID format)
            var possibleIds = new List<string?> { userIdFromSub, userIdFromNameId, userIdFromUserid }
                .Concat(allNameIdentifierClaims)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();
                
            foreach (var id in possibleIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                
                // Check if it looks like a GUID (prioritize these)
                if (Guid.TryParse(id, out _))
                {
                    user = await _userManager.FindByIdAsync(id);
                    if (user != null)
                    {
                        actualUserId = id;
                        _logger.LogInformation("[AuthAPIController] Found user by GUID ID: {ActualUserId}", actualUserId);
                        break;
                    }
                }
            }
            
            // If still not found, try by username
            if (user == null && !string.IsNullOrEmpty(username))
            {
                user = await _userManager.FindByNameAsync(username);
                if (user != null)
                {
                    actualUserId = user.Id;
                    _logger.LogInformation("[AuthAPIController] Found user by username: {Username}, ID: {ActualUserId}", username, actualUserId);
                }
            }
            
            // Try non-GUID IDs as last resort
            if (user == null)
            {
                foreach (var id in possibleIds)
                {
                    if (string.IsNullOrEmpty(id) || Guid.TryParse(id, out _)) continue;
                    
                    user = await _userManager.FindByIdAsync(id);
                    if (user != null)
                    {
                        actualUserId = user.Id;
                        _logger.LogInformation("[AuthAPIController] Found user by non-GUID ID: {Id} -> Actual ID: {ActualUserId}", id, actualUserId);
                        break;
                    }
                }
            }
            
            if (user == null)
            {
                _logger.LogWarning("[AuthAPIController] delete account failed - user not found with any ID: {PossibleIds} or username: {Username}", 
                    string.Join(", ", possibleIds), username);
                return NotFound("User not found");
            }

            // Get user roles to determine what to delete
            var roles = await _userManager.GetRolesAsync(user);
            
            // Delete associated Patient or Employee records first (if they exist)
            foreach (var role in roles)
            {
                try
                {
                    if (role == "Patient")
                    {
                        var patients = await _patientRepository.GetAll();
                        var patient = patients.FirstOrDefault(p => p.UserId == actualUserId);
                        if (patient != null)
                        {
                            Console.WriteLine($"[AuthController] Deleting patient record for UserId: {actualUserId}");
                            await _patientRepository.Delete(patient.PatientId);
                        }
                        else
                        {
                            Console.WriteLine($"[AuthController] No patient record found for UserId: {actualUserId}");
                        }
                    }
                    else if (role == "Employee")
                    {
                        var employees = await _employeeRepository.GetAll();
                        var employee = employees.FirstOrDefault(e => e.UserId == actualUserId);
                        if (employee != null)
                        {
                            Console.WriteLine($"[AuthController] Deleting employee record for UserId: {actualUserId}");
                            await _employeeRepository.Delete(employee.EmployeeId);
                        }
                        else
                        {
                            Console.WriteLine($"[AuthController] No employee record found for UserId: {actualUserId}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AuthController] Error deleting {role} record: {ex.Message}");
                    // Continue with user deletion even if Patient/Employee deletion fails
                }
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("[AuthAPIController] user account deleted for {Username}", user.UserName);
                return Ok(new { Message = "Account deleted successfully" });
            }

            _logger.LogWarning("[AuthAPIController] delete account failed for {Username}", user.UserName);
            var errors = result.Errors.Select(e => new { code = e.Code, description = e.Description });
            return BadRequest(errors);
        }

        private async Task<string> GenerateJwtToken(AuthUser user)
        {
            var jwtKey = _configuration["Jwt:Key"]; // The secret key used for the signature
            if (string.IsNullOrEmpty(jwtKey)) // Ensure the key is not null or empty
            {   
                _logger.LogError("[AuthAPIController] JWT key is missing from configuration.");
                throw new InvalidOperationException("JWT key is missing from configuration.");
            }
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)); // Reading the key from the configuration
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256); // Using HMAC SHA256 algorithm for signing the token

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id), // Subject of the token - use USER ID not username
                new Claim(JwtRegisteredClaimNames.Email, user.Email!), // User's email
                new Claim(ClaimTypes.NameIdentifier, user.Id), // Unique identifier for the user - use USER ID
                new Claim(ClaimTypes.Name, user.UserName!), // Username
                new Claim("username", user.UserName!), // Explicit username claim
                new Claim("userid", user.Id), // Explicit user ID claim
                new Claim("nameid", user.Id), // Alternative user ID claim
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique identifier for the token
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()) // Issued at timestamp
            };

            // Add role claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role)); // Add explicit role claim for JWT
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(120), // Token expiration time set to 120 minutes
                signingCredentials: credentials); // Signing the token with the specified credentials

            _logger.LogInformation("[AuthAPIController] JWT token created for {@username} with roles {@roles}", user.UserName, string.Join(", ", roles));
            _logger.LogInformation("[AuthAPIController] JWT token claims: {Claims}", string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}")));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}