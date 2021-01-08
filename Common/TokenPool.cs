using System;
using System.Collections.Generic;

namespace MinecraftTunnel.Common
{
    public class TokenPool
    {
        private Stack<PlayerToken> tokenPool;
        public TokenPool(int capacity)
        {
            tokenPool = new Stack<PlayerToken>(capacity);
        }
        public void Push(PlayerToken playerToken)
        {
            if (playerToken == null)
            {
                throw new ArgumentException("Items added to a PlayerToken cannot be null");
            }
            lock (tokenPool)
            {
                tokenPool.Push(playerToken);
            }
        }
        public PlayerToken Pop()
        {
            lock (tokenPool)
            {
                return tokenPool.Pop();
            }
        }
        public int Count
        {
            get { return tokenPool.Count; }
        }
    }
}
