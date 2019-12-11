using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;

namespace NewLife.Cube.Session
{
    public class MemorySessionStore : ISessionStore
    {
        public ISession Create(String sessionKey, TimeSpan idleTimeout, TimeSpan ioTimeout, Func<Boolean> tryEstablishSession, Boolean isNewSessionKey)
        {
            throw new NotImplementedException();
        }
    }
}