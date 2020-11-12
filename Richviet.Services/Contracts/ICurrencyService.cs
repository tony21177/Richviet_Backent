﻿using Richviet.Services.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Richviet.Services.Contracts
{
    public interface ICurrencyService
    {
        List<CurrencyCode> GetCurrencyByCountry(string country);
        
        public CurrencyCode GetCurrencyById(int id);
        
        List<CurrencyCode> GetCurrencyList();

        bool AddCurrency(CurrencyCode currency);

        bool ModifyCurrency(CurrencyCode modifyCurrency);

        bool DeleteCurrency(int id);
    }
}
