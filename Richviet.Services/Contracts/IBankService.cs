﻿
using Richviet.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Richviet.Services.Contracts
{
    public interface IBankService
    {
        List<ReceiveBank> GetReceiveBanks();

        void AddReceiveBank(ReceiveBank bank);

        void ModifyReceiveBank(ReceiveBank modifyBank, ReceiveBank oldBank);

        void DeleteReceiveBank(ReceiveBank bank);
    }
}
