using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SignaLR_Apis.Models;
using SignaLR_Apis.MongoDB;
using SignaLR_Apis.JwtHelpers;
using MongoDB.Bson;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SignaLR_Apis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly MongoDBService dbService;
        private readonly JwtHelper jwtHelper;

        public AuthController(MongoDBService dbService, JwtHelper jwtHelper)
        {
            this.dbService = dbService;
            this.jwtHelper = jwtHelper;
        }


        [HttpGet("users")]
        //[Authorize]
        public async Task<IActionResult> GetAllUsers() =>
             Ok((await dbService.GetCollection<UserModel>("users").FindAsync(_ => true)).ToList());


        [HttpGet("activeUsers")]
        public async Task<IActionResult> GetActiveUsers() =>
            Ok((await dbService.GetCollection<ActiveUserModel>("activeUserss").FindAsync(_ => true)).ToList());

        [HttpDelete("deleteActiveUsers")]
        public async Task<IActionResult> DeleteActiveUsers() =>
            Ok((await dbService.GetCollection<ActiveUserModel>("activeUserss").DeleteManyAsync(_ => true)));

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync(LoginModel model)
        {
            if (string.IsNullOrEmpty(model.Email) && string.IsNullOrEmpty(model.Password))
                return BadRequest("invalid data");

            var userCollection = dbService.GetCollection<UserModel>("users");
            var user = (await userCollection.FindAsync(u => u.Email == model.Email && u.Password == model.Password)).FirstOrDefault();

            if (user is null)
                return NotFound("user is not exsists");

            var token = jwtHelper.GenerateToken(user);
            return Ok(new LoginResultModel()
            {
                Id = user.Id,
                AuthToken = token,
            });
        }



        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync(ResgisterModel model)
        {
            if ((string.IsNullOrEmpty(model.Email)
            && string.IsNullOrEmpty(model.Password)
            && string.IsNullOrEmpty(model.Surname)
            && string.IsNullOrEmpty(model.Name)))
                return BadRequest("invalid data");

            if ((await dbService.GetCollection<UserModel>("users").FindAsync(u => u.Email == model.Email)).FirstOrDefault() is not null)
                return BadRequest("email already exsists");
            var userCollection = dbService.GetCollection<UserModel>("users");
            await userCollection.InsertOneAsync(new UserModel()
            {
                Id = Guid.NewGuid().ToString(),
                Name = model.Name,
                Email = model.Email,
                Password = model.Password,
                Surname = model.Surname,
            });
            return Ok("created user");
        }

    }
}

