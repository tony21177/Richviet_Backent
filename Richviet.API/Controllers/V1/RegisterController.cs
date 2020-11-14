﻿using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Richviet.API.DataContracts.Dto;
using Richviet.API.DataContracts.Responses;
using Richviet.Services.Contracts;
using Frontend.DB.EF.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections;
using AutoMapper;
using Richviet.API.DataContracts.Requests;
using Richviet.Tools.Utility;
using Richviet.Task;
using Microsoft.Extensions.Logging;
using Richviet.Services.Constants;
using System.Net;

namespace Richviet.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/register")]
    [ApiController]
    [Authorize]
    public class RegisterController : Controller
    {
        private readonly IUserService userService;
        private readonly JwtHandler jwtHandler;
        private readonly IMapper mapper;
        private readonly ArcValidationTask arcValidationTask;

        public RegisterController(IUserService userService, JwtHandler jwtHandler, IMapper mapper, ArcValidationTask arcValidationTask)
        {
            this.userService = userService;
            this.jwtHandler = jwtHandler;
            this.mapper = mapper;
            this.arcValidationTask = arcValidationTask;
        }

        /// <summary>
        /// 註冊使用者相關資訊
        /// </summary>
        [HttpPut("register")]
        public ActionResult<MessageModel<RegisterResponseDTO>> ModifyOwnUserInfo([FromBody] RegisterRequest registerReq)
        {
            UserInfoDTO userModel = null;
            Tools.Utility.TokenResource accessToken = null;

            
            var userId = int.Parse(User.FindFirstValue("id"));
            UserArc userArc = userService.GetUserArcById(userId);
            if (userArc.KycStatus != (byte)KycStatusEnum.DRAFT_MEMBER)
            {
                return BadRequest(new MessageModel<RegisterResponseDTO>
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Success = false,
                    Msg = "Only Draft member can register"
                }
                );
            }
            

            bool isRegister = userService.ReigsterUserById(userId, registerReq);

            if (isRegister == false)
            {
                return BadRequest();
            }

            UserInfoView userInfo = userService.GetUserInfoById(userId);
            //// 將 user 置換成 ViewModel
            userModel = mapper.Map<UserInfoDTO>(userInfo);

            accessToken = jwtHandler.CreateAccessToken(userModel.Id, userModel.Email, userModel.ArcName);
            

            //return Ok(new MessageModel<UserInfoDTO>
            //{
            //    Data = userModel
            //});

            return Ok(new MessageModel<RegisterResponseDTO>
            {
                Data = new RegisterResponseDTO
                {
                    Jwt = accessToken.Token,
                    kycStatus = (byte)userModel.KycStatus
                }
            });
        }

        [HttpGet("validation")]
        public ActionResult<MessageModel<Object>> Validation()
        {
            
            var isVerified = arcValidationTask.Validate("XXXXXXXX", "20180101", "", "F160000001");

            return Ok();
        }
        
    } 
}
