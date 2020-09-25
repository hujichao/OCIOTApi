using CH.Common.RedisHelp;
using System;
using System.Configuration;

namespace OCIOTApi.Common
{
    public interface IRedis
    {
        bool HashSet(string RedisKey, string key, string value);
        bool HashDelete(string RedisKey, string key);
    }

    public class SimpleChannel : IRedis
    {
        static RedisHelper redisHelper;

        public SimpleChannel()
        {
            CheckConnect();
            //return channelClient;
        }

        public bool HashSet(string RedisKey, string key, string value)
        {
            CheckConnect();
            return redisHelper.HashSet(RedisKey, key, value);
        }

        public bool HashDelete(string RedisKey, string key)
        {
            CheckConnect();
            return redisHelper.HashDelete(RedisKey, key);
        }
        
        private void CheckConnect()
        {
            if (redisHelper == null)
            {
                string urlGetRun = ConfigurationManager.AppSettings["FuelGasRedisConnection"];
                string db = ConfigurationManager.AppSettings["FuelGasRedisConnectionDb"];
                redisHelper = new RedisHelper(urlGetRun, Convert.ToInt32(db));
            }
        }


    }
}
