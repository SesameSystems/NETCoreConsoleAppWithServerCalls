﻿using System;

namespace NETCoreConsoleAppWithServerCalls
{

    [Serializable]
    public class KeyAndValueItem
    {

        public KeyAndValueItem()
        {
            Id = string.Empty;
            Name = string.Empty;
        }

        public string Id { get; set; }

        public string Name { get; set; }

    }

}
