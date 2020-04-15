namespace GameBox.Framework
{
    using System;

    public class BoxBaseOptions : IOptions
    {
        public TimeSpan IdleTimeout { get; set; }
    }
}