﻿using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Frontend.DB.EF.Models;
using Microsoft.EntityFrameworkCore;
using Z.EntityFramework.Plus;

namespace Users.Domains.Users.Command.Adapter.Repositories
{
    public class UserCommandDbCommandRepository : IUserCommandRepository
    {
        private readonly GeneralContext _context;

        public UserCommandDbCommandRepository(GeneralContext context)
        {
            _context = context;
        }

        public void Modify(User user)
        {
            _context.Entry(user).State = EntityState.Modified;
            _context.SaveChanges();
        }


        public int DeleteDraftUser()
        {
            int numberOfEffectRow = _context.User.Where(x => x.UserArc.KycStatus == 0).DeleteFromQuery();

            _context.SaveChanges();
            return numberOfEffectRow;
        }

        public int DeleteDraftUserAfterDays(int days)
        {
            DateTime now = DateTime.UtcNow;
            //int numberOfEffectRow = _context.User.Where(x => x.UserArc.KycStatus == 0 && now.Subtract(x.UserArc.CreateTime)).DeleteFromQuery();

            _context.SaveChanges();
            return 0;
        }
    }
}
