using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.Database;
using Store.DTOs;
using Store.Services;

namespace Store.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JWTService _jwtService;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public UsersController(
            AppDbContext appDbContext,
            JWTService jwtService,
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager)
        {
            _context = appDbContext;
            _jwtService = jwtService;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginDTO model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName);
            if(user is null)
            {
                return Unauthorized("Invalid username or password");
            }
            if(user.EmailConfirmed == false)
            {
                return Unauthorized("Please confirm your email");
            }
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized("Invalid username or password");
            }
            return new LoginResponseDto
            {
                Username = model.UserName,
                Token = _jwtService.CreateJWT(user)
            };
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(LoginDTO model)
        {
            if (await CheckEmailExistsAsync(model.Email))
            {
                return BadRequest("There is an account with the same email");
            }
            var userToAdd = new IdentityUser {
                UserName = model.UserName,
                Email = model.Email,
                EmailConfirmed = true,
            };
            var result = await _userManager.CreateAsync(userToAdd, model.Password);
            if (!result.Succeeded) {
                return BadRequest(result.Errors);
            }
            return Ok();
        }

        private async Task<bool> CheckEmailExistsAsync(string email)
        {
            return await _userManager.Users.AnyAsync(u => u.Email == email.ToLower());
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IdentityUser>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<IdentityUser>> Get(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // POST: api/users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<AddUserDto>> PostUser(AddUserDto user)
        {
            _context.Users.Add(new IdentityUser
            {
                UserName = user.Name
            });
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUsers", new { name = user.Name }, user);
        }

        // DELETE: api/users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Deleteuser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
