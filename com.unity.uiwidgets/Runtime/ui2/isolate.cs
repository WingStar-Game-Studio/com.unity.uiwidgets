﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.UIWidgets.foundation;

namespace Unity.UIWidgets.ui2 {
    public class Isolate {
        static Dictionary<IntPtr, Isolate> _isolates = new Dictionary<IntPtr, Isolate>();

        Isolate(IntPtr ptr) {
            _ptr = ptr;
        }

        IntPtr _ptr;

        public bool isValid => _ptr != IntPtr.Zero;

        public static Isolate current {
            get {
                IntPtr ptr = ensureExists();
                if (_isolates.TryGetValue(ptr, out Isolate value)) {
                    D.assert(value.isValid);
                    return value;
                }

                var isolate = new Isolate(ptr);
                _isolates.Add(ptr, isolate);
                return isolate;
            }
        }

        public static IntPtr ensureExists() {
            IntPtr ptr = Isolate_current();
            if (ptr == IntPtr.Zero) {
                throw new Exception("Isolate.current is null. " +
                                    "This usually happens when there is a callback from outside of UIWidgets. " +
                                    "Try to use \"using (Isolate.getScope(...)) { ... }\" to wrap your code.");
            }

            return ptr;
        }

        internal static void remove(Isolate isolate) {
            D.assert(isolate != null && isolate.isValid);
            _isolates.Remove(isolate._ptr);
            isolate._ptr = IntPtr.Zero;
        }

        public static IDisposable getScope(Isolate isolate) {
            D.assert(isolate != null && isolate.isValid);
            return new _IsolateDisposable(isolate._ptr);
        }

        class _IsolateDisposable : IDisposable {
            IntPtr _isolate;
            IntPtr _previous;

            public _IsolateDisposable(IntPtr isolate) {
                _isolate = isolate;
                _previous = Isolate_current();
                if (_previous == _isolate) {
                    return;
                }

                if (_previous != IntPtr.Zero) {
                    Isolate_exit();
                }

                Isolate_enter(_isolate);
            }

            public void Dispose() {
                var current = Isolate_current();
                D.assert(current == IntPtr.Zero || current == _isolate);
                if (_previous == _isolate) {
                    return;
                }

                if (current != IntPtr.Zero) {
                    Isolate_exit();
                }

                if (_previous != IntPtr.Zero) {
                    Isolate_enter(_previous);
                }
            }
        }


        [DllImport(NativeBindings.dllName)]
        static extern IntPtr Isolate_current();

        [DllImport(NativeBindings.dllName)]
        static extern void Isolate_enter(IntPtr isolate);

        [DllImport(NativeBindings.dllName)]
        static extern void Isolate_exit();
    }
}