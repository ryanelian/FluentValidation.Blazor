using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Demo.Forms.Services
{
    public class EmailCheckerService
    {
        public async Task<bool> IsAvailableAsync(string email)
        {
            // simulate delay accessing remote services
            await Task.Delay(1000);

            if (email == "ryan.elian@accelist.com")
            {
                return await Task.FromResult(false);
            }

            return await Task.FromResult(true);
        }
    }
}
