using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Demo.Services
{
    public class EmailCheckerService
    {
        public async Task<bool> IsAvailableAsync(string email)
        {
            await Task.Delay(1000);

            if (email == "ryan.elian@accelist.com")
            {
                return await Task.FromResult(false);
            }

            return await Task.FromResult(true);
        }
    }
}
