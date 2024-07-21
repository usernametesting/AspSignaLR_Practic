using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using SignaLR_Apis.Models;
using SignaLR_Apis.MongoDB;
using System.Collections.Concurrent;

namespace SignaLR_Apis.SignaLR
{

    public class ChatHub : Hub
    {
        private static List<ActiveUserModel> activeUsers = new();
        private readonly MongoDBService service;

        public ChatHub(MongoDBService service)
        {
            this.service = service;
        }
        public override async Task OnConnectedAsync()
        {
            string Id = Context?.GetHttpContext()?.Request?.Query["userId"]!;


            UserModel user = (await service.GetCollection<UserModel>("users")
                          .FindAsync(u => u.Id == Id)).FirstOrDefault();
            
            var activeUser = new ActiveUserModel()
            {
                Id = Guid.NewGuid().ToString(),
                ConnectionId = Context.ConnectionId,
                UserId = user.Id,
                UserName = user.Name
            
            };
            await service.GetCollection<ActiveUserModel>("activeUserss").InsertOneAsync(activeUser);

            await Clients.AllExcept(Context.ConnectionId).SendAsync("UserConnected",activeUser);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await service.GetCollection<ActiveUserModel>("activeUserss").DeleteOneAsync(u => u.ConnectionId == Context.ConnectionId);
            await Clients.AllExcept(Context.ConnectionId).SendAsync("UserDisconnected", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task<List<ActiveUserModel>> GetActiveUsers() =>
             (await service.GetCollection<ActiveUserModel>("activeUserss")
                 .FindAsync(u => u.ConnectionId != Context.ConnectionId)).ToList();

        public async Task SendMessage(string toUserId, string message)
        {
            var fromUser = (await service.GetCollection<ActiveUserModel>("activeUserss").FindAsync(u => u.ConnectionId == Context.ConnectionId)).First();

            string toUserConnectionId
                = (await service.GetCollection<ActiveUserModel>("activeUserss").FindAsync(u => u.UserId == toUserId)).First().ConnectionId;

            await Clients.Client(toUserConnectionId).SendAsync("ReceiveMessage", fromUser.UserName, message);
        }
    }
}
