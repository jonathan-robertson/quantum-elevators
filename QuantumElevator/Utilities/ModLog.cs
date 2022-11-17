﻿using System;

namespace QuantumElevator.Utilities {
    internal class ModLog<T> {
        private readonly string className;

        public ModLog() {
            className = typeof(T).FullName;
        }

        public void Trace(string message) {
            Log.Out($"[{className}] TRACE: {message}");
        }

        public void Debug(string message) {
            Log.Out($"[{className}] DEBUG: {message}");
        }

        public void Info(string message) {
            Log.Out($"[{className}] {message}");
        }

        public void Warn(string message, Exception e = null) {
            Log.Warning($"[{className}] {message}");
            if (e != null) {
                Log.Warning($"[{className}] {message}\n{e.Message}\n{e.StackTrace}");
                Log.Exception(e);
            }
        }

        public void Error(string message, Exception e = null) {
            Log.Error($"[{className}] {message}");
            if (e != null) {
                Log.Error($"[{className}] {message}\n{e.Message}\n{e.StackTrace}");
                Log.Exception(e);
            }
        }
    }
}
