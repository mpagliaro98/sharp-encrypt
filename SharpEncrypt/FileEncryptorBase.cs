﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpEncrypt
{
    public abstract class FileEncryptorBase
    {
        protected const string EXT_ENCRYPTED = ".senc";

        public abstract string Filepath { get; }
        public abstract bool WorkComplete { get; }

        public abstract bool ContainsFile(string filepath);
        public abstract bool Encrypt(string password, bool encryptFilename, WorkTracker tracker);
        public abstract bool Decrypt(string password, WorkTracker tracker);
    }
}