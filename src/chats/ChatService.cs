// DTO for chat with messages and participants

using Comms.Data;
using Comms.Helpers;
using Comms.Models;
using Comms.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Profile;

namespace Comms.Services
{
    public class ChatService
    {
        private readonly IProfileGrpcService _profileService;

        private readonly CommunicationsDbContext _context;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            CommunicationsDbContext context,
            ILogger<ChatService> logger,
            IProfileGrpcService profileService
        )
        {
            _context = context;
            _logger = logger;
            _profileService = profileService;
        }

        public async Task<object?> GetChatHistoryAsync(Guid chatId)
        {
            try
            {
                var chat = await _context
                    .Chats.Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                    .Include(c => c.Participants)
                    .FirstOrDefaultAsync(c => c.ChatId == chatId);

                if (chat == null)
                    throw new Exception("Chat not found");

                // Only include the last sent message (most recent by CreatedAt)
                var lastMessage = chat
                    .Messages.OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault();
                var messageDtos = new List<MessageWithSenderDto>();
                if (lastMessage != null)
                {
                    object? sender = null;
                    var userResponse = await _profileService.GetUserAsync(
                        lastMessage.SenderId.ToString() ?? ""
                    );
                    sender = userResponse.User;
                    messageDtos.Add(
                        new MessageWithSenderDto
                        {
                            MessageId = lastMessage.MessageId,
                            Content = lastMessage.Content,
                            ChatId = lastMessage.ChatId,
                            SenderId = lastMessage.SenderId,
                            Sender = sender,
                            Type = lastMessage.Type,
                            Status = lastMessage.Status,
                            MediaUrls = lastMessage.MediaUrls,
                            UpdatedAt = lastMessage.UpdatedAt,
                            CreatedAt = lastMessage.CreatedAt,
                            DeletedAt = lastMessage.DeletedAt,
                            Duration = lastMessage.Duration,
                            FileUrl = lastMessage.FileUrl,
                            // Map other fields as needed
                        }
                    );
                }
                var participants_dto = new List<ParticipantWithUserDto>();

                // Attach user info to participants
                foreach (var participant in chat.Participants)
                {
                    var userResponse = await _profileService.GetUserAsync(
                        participant.UserId.ToString()
                    );
                    participants_dto.Add(
                        new ParticipantWithUserDto
                        {
                            ChatId = participant.ChatId,
                            UserId = participant.UserId,
                            JoinedAt = participant.JoinedAt,
                            IsAdmin = participant.IsAdmin,
                            IsActive = participant.IsActive,
                            LeftAt = participant.LeftAt,
                            UnreadCount = participant.UnreadCount,
                            Chat = participant.Chat,
                        }
                    );
                }

                // Build chat DTO
                var chatDto = new ChatHistoryDto
                {
                    ChatId = chat.ChatId,
                    Name = chat.Name,
                    AvatarUrl = chat.AvatarUrl,
                    ChatType = chat.ChatType,
                    UpdatedAt = chat.UpdatedAt,
                    Messages = messageDtos,
                    Participants = participants_dto,
                };

                if (chat.ChatType == Models.ChatType.DIRECT)
                {
                    // For direct chats, set the avatar to the other participant's avatar
                    var otherParticipant = chat.Participants.FirstOrDefault();
                    if (otherParticipant != null)
                    {
                        var userResponse = await _profileService.GetUserAsync(
                            otherParticipant.UserId.ToString()
                        );
                        chatDto.AvatarUrl = userResponse.User?.AvatarUrl;
                    }
                }
                return chatDto;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting chat history: {ex.Message}");
                throw new Exception("Failed to get chat history");
            }
        }

        public async Task<List<object>> GetMessagesAsync(Guid chatId)
        {
            try
            {
                var messages = await _context
                    .Messages.Where(m => m.ChatId == chatId)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                var messageDtos = new List<MessageWithSenderDto>();
                foreach (var message in messages)
                {
                    object? sender = null;
                    var userResponse = await _profileService.GetUserAsync(
                        message.SenderId.ToString() ?? ""
                    );
                    sender = userResponse.User;
                    messageDtos.Add(
                        new MessageWithSenderDto
                        {
                            MessageId = message.MessageId,
                            Content = message.Content,
                            ChatId = message.ChatId,
                            SenderId = message.SenderId,
                            Sender = sender,
                            Type = message.Type,
                            Status = message.Status,
                            MediaUrls = message.MediaUrls,
                            UpdatedAt = message.UpdatedAt,
                            CreatedAt = message.CreatedAt,
                            DeletedAt = message.DeletedAt,
                            Duration = message.Duration,
                            FileUrl = message.FileUrl,
                            // Map other fields as needed
                        }
                    );
                }

                return messageDtos.Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting messages: {ex.Message}");
                throw new Exception("Failed to get messages");
            }
        }

        public async Task<bool> IsParticipantAsync(string chatId, string userId)
        {
            var participant = await _context.ChatParticipant.FindAsync(chatId, userId);

            return participant != null;
        }

        public async Task LeaveChatAsync(string chatId, string userId)
        {
            var participants = await _context
                .ChatParticipant.Where(cp =>
                    cp.ChatId == Guid.Parse(chatId) && cp.UserId == Guid.Parse(userId)
                )
                .ToListAsync();

            _context.ChatParticipant.RemoveRange(participants);
            await _context.SaveChangesAsync();
        }

        public async Task<Message> UpdateMessageStatusAsync(
            string messageId,
            MessageStatus toStatus
        )
        {
            try
            {
                var message = await _context.Messages.FirstOrDefaultAsync(m =>
                    m.MessageId == Guid.Parse(messageId)
                );

                if (message == null)
                    throw new Exception("Message not found");

                message.Status = toStatus;
                _context.Messages.Update(message);
                await _context.SaveChangesAsync();

                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating message status: {ex.Message}");
                throw new Exception("Failed to update message status");
            }
        }

        public async Task<IEnumerable<ChatHistoryDto>> GetAllChatsWithLastMessageAsync(
            string UserId
        )
        {
            try
            {
                var chats = await _context
                    .Chats.Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                    .Where(c => c.Participants.Any(p => p.UserId.ToString() == UserId))
                    .Include(c => c.Participants)
                    .ToListAsync();

                var chatDtos = new List<ChatHistoryDto>();

                foreach (var chat in chats)
                {
                    var lastMessage = chat
                        .Messages.OrderByDescending(m => m.CreatedAt)
                        .FirstOrDefault();

                    // Build message DTO (if message exists)
                    var messageDtos = new List<MessageWithSenderDto>();
                    if (lastMessage != null)
                    {
                        UserResponse? userResponse = null;
                        if (lastMessage.SenderId != null)
                        {
                            userResponse = await _profileService.GetUserAsync(
                                lastMessage.SenderId.ToString() ?? ""
                            );
                        }

                        messageDtos.Add(
                            new MessageWithSenderDto
                            {
                                MessageId = lastMessage.MessageId,
                                Content = lastMessage.Content,
                                ChatId = lastMessage.ChatId,
                                SenderId = lastMessage.SenderId,
                                Sender = userResponse?.User,
                                Type = lastMessage.Type,
                                Status = lastMessage.Status,
                                MediaUrls = lastMessage.MediaUrls,
                                UpdatedAt = lastMessage.UpdatedAt,
                                CreatedAt = lastMessage.CreatedAt,
                                DeletedAt = lastMessage.DeletedAt,
                                Duration = lastMessage.Duration,
                                FileUrl = lastMessage.FileUrl,
                            }
                        );
                    }

                    // Build participant DTOs
                    var participantsDto = new List<ParticipantWithUserDto>();
                    foreach (var participant in chat.Participants)
                    {
                        var userResponse = await _profileService.GetUserAsync(
                            participant.UserId.ToString()
                        );
                        participantsDto.Add(
                            new ParticipantWithUserDto
                            {
                                ChatId = participant.ChatId,
                                UserId = participant.UserId,
                                JoinedAt = participant.JoinedAt,
                                IsAdmin = participant.IsAdmin,
                                IsActive = participant.IsActive,
                                LeftAt = participant.LeftAt,
                                UnreadCount = participant.UnreadCount,
                                Chat = participant.Chat,
                            }
                        );
                    }

                    // Build final Chat DTO
                    var chatDto = new ChatHistoryDto
                    {
                        ChatId = chat.ChatId,
                        Name = chat.Name,
                        AvatarUrl = chat.AvatarUrl,
                        ChatType = chat.ChatType,
                        UpdatedAt = chat.UpdatedAt,
                        Messages = messageDtos,
                        Participants = participantsDto,
                    };

                    // For direct chats — show the other participant's avatar
                    if (chat.ChatType == Models.ChatType.DIRECT)
                    {
                        var otherParticipant = chat.Participants.FirstOrDefault();
                        if (otherParticipant != null)
                        {
                            var userResponse = await _profileService.GetUserAsync(
                                otherParticipant.UserId.ToString()
                            );
                            chatDto.AvatarUrl = userResponse.User?.AvatarUrl;
                        }
                    }

                    chatDtos.Add(chatDto);
                }

                return chatDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all chat histories: {ex.Message}");
                throw new Exception("Failed to get all chats");
            }
        }

        public async Task<object> SendMessage(SendMessageDto sendMessageDto)
        {
            try
            {
                var duration = 0;
                if (sendMessageDto.Type == MessageType.IMAGE)
                {
                    foreach (var url in sendMessageDto.MediaUrls)
                    {
                        if (!new FileValidator().IsValidImageUrl(url))
                        {
                            throw new Exception("Invalid image URL: " + url);
                        }
                    }
                }
                else if (sendMessageDto.Type == MessageType.AUDIO)
                {
                    if (
                        sendMessageDto.MediaUrls.Length != 1
                        || !new FileValidator().IsAudioFile(sendMessageDto.MediaUrls[0])
                    )
                    {
                        throw new Exception("Invalid audio file URL");
                    }
                    duration = sendMessageDto.Duration ?? 0;
                }

                var message = new Message
                {
                    MessageId = Guid.NewGuid(),
                    ChatId = Guid.Parse(sendMessageDto.ChatId),
                    SenderId = Guid.Parse(sendMessageDto.SenderId),
                    Content = sendMessageDto.Content,
                    Type = sendMessageDto.Type,
                    MediaUrls = sendMessageDto.MediaUrls,
                    Duration = duration,
                    Status = MessageStatus.SENT,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                _context.Messages.Add(message);

                // Update chat's UpdatedAt
                var chat = await _context.Chats.FirstOrDefaultAsync(c =>
                    c.ChatId == Guid.Parse(sendMessageDto.ChatId)
                );
                if (chat != null)
                {
                    chat.UpdatedAt = DateTime.UtcNow;
                }

                // Increment unread count for other participants
                var participants = await _context
                    .ChatParticipant.Where(p =>
                        p.ChatId == Guid.Parse(sendMessageDto.ChatId)
                        && p.UserId != Guid.Parse(sendMessageDto.SenderId)
                        && p.IsActive
                    )
                    .ToListAsync();
                foreach (var participant in participants)
                {
                    participant.UnreadCount += 1;
                }

                await _context.SaveChangesAsync();

                return new { messageId = message.MessageId.ToString(), status = "Message sent" };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending message: {ex.Message}");
                throw new Exception("Failed to send message");
            }
        }

        public async Task<CreateChatReponseDto> CreateDirectChatAsync(
            string creatorId,
            string participantId
        )
        {
            try
            {
                var existingChat = await _context
                    .Chats.Include(c => c.Participants)
                    .FirstOrDefaultAsync(c =>
                        c.ChatType == ChatType.DIRECT
                        && c.Participants.Any(p => p.UserId.ToString() == creatorId)
                        && c.Participants.Any(p => p.UserId.ToString() == participantId)
                    );

                if (existingChat != null)
                {
                    return new CreateChatReponseDto
                    {
                        ChatId = existingChat.ChatId.ToString(),
                        Message = "Direct chat between useralready exists",
                        Success = false,
                    };
                }

                var text = $"Direct chat between {creatorId} and {participantId}";
                var chat_created = await CreateChatAsync(
                    new CreateChatDto
                    {
                        CreatorId = creatorId,
                        ParticiantsIds = new string[] { participantId },
                        ChatType = ChatType.DIRECT,
                    }
                );
                return new CreateChatReponseDto
                {
                    ChatId = chat_created,
                    Message = text,
                    Success = true,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating direct chat: {ex.Message}");
                throw new Exception("Failed to create direct chat");
            }
        }

        public async Task<CreateChatReponseDto> CreateGroupChatAsync(CreateGroupChatDto data)
        {
            try
            {
                var chat_created = await CreateChatAsync(
                    new CreateChatDto
                    {
                        CreatorId = data.CreatorId,
                        ParticiantsIds = data.ParticipantIds,
                        ChatType = ChatType.GROUP,
                        ChatName = data.ChatName,
                        AvatarUrl = data.AvatarUrl,
                    }
                );
                var text = $"Group chat '{data.ChatName}' created by {data.CreatorId}";
                return new CreateChatReponseDto
                {
                    ChatId = chat_created,
                    Message = text,
                    Success = true,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating group chat: {ex.Message}");
                throw new Exception("Failed to create group chat");
            }
        }

        private async Task<string> CreateChatAsync(CreateChatDto createChatDto)
        {
            try
            {
                var chat = new Chat
                {
                    ChatId = Guid.NewGuid(),
                    Name = createChatDto.ChatName ?? "",
                    AvatarUrl = createChatDto.AvatarUrl ?? "",
                    ChatType = createChatDto.ChatType,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                _context.Chats.Add(chat);

                var creator = new ChatParticipant
                {
                    ChatId = chat.ChatId,
                    UserId = Guid.Parse(createChatDto.CreatorId),
                    IsAdmin = false,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true,
                    UnreadCount = 0,
                };
                _context.ChatParticipant.Add(creator);
                // Add participants
                foreach (var participantDto in createChatDto.ParticiantsIds)
                {
                    var participant = new ChatParticipant
                    {
                        ChatId = chat.ChatId,
                        UserId = Guid.Parse(participantDto),
                        IsAdmin = false,
                        JoinedAt = DateTime.UtcNow,
                        IsActive = true,
                        UnreadCount = 0,
                    };
                    _context.ChatParticipant.Add(participant);
                }

                await _context.SaveChangesAsync();

                var Message = new Message
                {
                    MessageId = Guid.NewGuid(),
                    ChatId = chat.ChatId,
                    Content = "Chat created",
                    Type = MessageType.SYSTEM,
                    Status = MessageStatus.SENT,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                _context.Messages.Add(Message);
                return chat.ChatId.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating chat: {ex.Message}");
                throw new Exception("Failed to create chat");
            }
        }
    }
}
