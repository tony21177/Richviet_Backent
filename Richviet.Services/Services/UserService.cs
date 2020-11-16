﻿using Microsoft.EntityFrameworkCore;
using Richviet.Services.Constants;
using Richviet.Services.Contracts;
using Frontend.DB.EF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Richviet.Tools.Utility;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Richviet.BackgroudTask.Arc.Vo;
using Richviet.BackgroudTask.Arc;

namespace Richviet.Services
{
    public class UserService: IUserService
    {
        private readonly IEnumerable<IAuthService> authServices;
        private readonly IArcScanRecordService arcScanRecordService;
        private readonly GeneralContext dbContext;
        private readonly ILogger logger;
        private readonly IMapper mapper;
        private readonly ArcValidationTask arcValidationTask;



        public UserService(IEnumerable<IAuthService> authServices, IArcScanRecordService arcScanRecordService, GeneralContext dbContext, ILogger<UserService> logger, IMapper mapper, ArcValidationTask arcValidationTask)
        {
            this.authServices = authServices;
            this.arcScanRecordService = arcScanRecordService;
            this.dbContext = dbContext;
            this.logger =  logger;
            this.mapper = mapper;
            this.arcValidationTask =  arcValidationTask;
        }

        public async Task<bool> AddNewUserInfo(UserRegisterType loginUser)
        {
            using var transaction = dbContext.Database.BeginTransaction();
            try
            {
                var user = new User();
                await dbContext.User.AddAsync(user);
                dbContext.SaveChanges();
                var userArc = new UserArc()
                {
                    UserId = user.Id
                };
                await dbContext.UserArc.AddAsync(userArc);
                dbContext.SaveChanges();


                var userRegisterType = new UserRegisterType()
                {
                    UserId = user.Id,
                    AuthPlatformId = loginUser.AuthPlatformId,
                    RegisterType = loginUser.RegisterType,
                    Email = loginUser.Email,
                    Name = loginUser.Name
                };
                await dbContext.UserRegisterType.AddAsync(userRegisterType);
                dbContext.SaveChanges();

                // Commit transaction if all commands succeed, transaction will auto-rollback
                // when disposed if either commands fails
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,null);
                transaction.Rollback();
                return false;
            }
        }

        public User GetUserById(int id)
        {
            return dbContext.User.Where(user => user.Id == id).FirstOrDefault();
        }

        public UserArc GetUserArcById(int userId)
        {
            return dbContext.UserArc.Where(userArc => userArc.UserId == userId).FirstOrDefault();
        }

        public UserRegisterType GetUserRegisterTypeById(int userId)
        {
            return dbContext.UserRegisterType.Where(userRegisterType => userRegisterType.UserId == userId).FirstOrDefault();
        }
        public async Task<UserInfoView> GetUserInfo(UserRegisterType loginUser)
        {

            var list = await dbContext.UserInfoView.Where(user => user.AuthPlatformId == loginUser.AuthPlatformId && user.RegisterType == loginUser.RegisterType).ToListAsync();

            var loggedingUser = list.FirstOrDefault();
            return loggedingUser;
        }

        public UserInfoView GetUserInfoById(int id)
        {
            return dbContext.UserInfoView.Where(userInfo => userInfo.Id == id).FirstOrDefault();
        }

        public bool ReigsterUser(User user,UserArc userArc,UserRegisterType userRegisterType)
        {
            dbContext.User.Update(user);
            dbContext.UserArc.Update(userArc);
            dbContext.UserRegisterType.Update(userRegisterType);

            dbContext.SaveChanges();

            return true;
        }

        public async Task<dynamic> VerifyUserInfo(string accessToken, string permissions, UserRegisterType loginUser)
        {

            switch ((LoginType)loginUser.RegisterType)
            {
                case LoginType.FB:
                    IAuthService authService = authServices.Single(service => service.LoginType == LoginType.FB);
                    return await authService.VerifyUserInfo(accessToken, permissions, loginUser);
                default:
                    return null;
            }
        }

        public UserArc UpdateUserArc(UserArc modifyUserArc,UserArc originalUserArc)
        {
            dbContext.Entry(originalUserArc).CurrentValues.SetValues(modifyUserArc);
            dbContext.Entry(originalUserArc).Property(x => x.UserId).IsModified = false;
            dbContext.Entry(originalUserArc).Property(x => x.CreateTime).IsModified = false;
            originalUserArc.UpdateTime = DateTime.UtcNow;
            dbContext.SaveChanges();
            return modifyUserArc;
        }

        public bool ChangeKycStatusByUserId(KycStatusEnum kycStatus, int userId)
        {
            User user = dbContext.User.Where(user => user.Id == userId).FirstOrDefault();
            UserArc userArc = dbContext.UserArc.Where(userArc => userArc.UserId == userId).FirstOrDefault();

            if (user == null || userArc == null)
            {
                logger.LogError("userId {userId} does not exists", userId);
                return false;
            }

            userArc.KycStatus = (byte)kycStatus;
            userArc.KycStatusUpdateTime = DateTime.UtcNow;


            dbContext.SaveChanges();

            return true;
        }

        public void UpdatePicFileNameOfUserInfo(UserArc userArc,Byte type,String fileName)
        {
            PictureTypeEnum pictureType = (PictureTypeEnum)type;
            switch (pictureType)
            {
                case PictureTypeEnum.Front:
                    userArc.IdImageA = fileName;
                    break;
                case PictureTypeEnum.Back:
                    userArc.IdImageB = fileName;
                    break;
            }
            dbContext.UserArc.Update(userArc);
            dbContext.SaveChanges();
        }

        public void SystemVerifyArc(int userId)
        {
            UserArc userArc = GetUserArcById(userId);
            if(userArc.ArcIssueDate == null || userArc.ArcExpireDate == null)
            {
                throw new Exception("ARC Data not sufficient");
            }
            ArcValidationResult arcValidationResult =  arcValidationTask.Validate(userArc.ArcNo,((DateTime)userArc.ArcIssueDate).ToString("yyyyMMdd"),((DateTime)userArc.ArcExpireDate).ToString("yyyyMMdd"),userArc.BackSequence).Result;
            logger.LogInformation("IsSuccessful:{1}", arcValidationResult.IsSuccessful);
            logger.LogInformation("result:{1}", arcValidationResult.Result);
            if (arcValidationResult.IsSuccessful)
            {
                ArcScanRecord record = new ArcScanRecord()
                {
                    ArcStatus = (byte)SystemArcVerifyStatusEnum.PASS,
                    ScanTime = DateTime.UtcNow,
                    Description = arcValidationResult.Result
                };
                userArc.KycStatus = (byte)KycStatusEnum.ARC_PASS_VERIFY;
                userArc.KycStatusUpdateTime = DateTime.UtcNow;
                arcScanRecordService.AddScanRecordForRegiterProcess(record,userArc);
            }
            else
            {
                ArcScanRecord record = new ArcScanRecord()
                {
                    ArcStatus = (byte)SystemArcVerifyStatusEnum.FAIL,
                    ScanTime = DateTime.UtcNow,
                    Description = arcValidationResult.Result
                };
                userArc.KycStatus = (byte)KycStatusEnum.FAILED_KYC;
                userArc.KycStatusUpdateTime = DateTime.UtcNow;
                arcScanRecordService.AddScanRecordForRegiterProcess(record, userArc);
            }
        }
    }
}
