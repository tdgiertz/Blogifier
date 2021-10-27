using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Blogifier.Admin.Components
{
    public class JsCallback
    {
        [JsonInclude]
        [JsonPropertyName("__isJsCallback")]
        public bool IsJsCallback { get; } = true;
        [JsonInclude]
        [JsonPropertyName("reference")]
        public object Reference { get; private set; }

        public static JsCallback Create(Func<ValueTask> callback)
        {
            return new JsCallback
            {
                Reference = DotNetObjectReference.Create(new JsValueAction(callback))
            };
        }

        public static JsCallback Create(Func<Task> callback)
        {
            return new JsCallback
            {
                Reference = DotNetObjectReference.Create(new JsAction(callback))
            };
        }

        public static JsCallback Create<T>(Func<T, Task> callback)
        {
            return new JsCallback
            {
                Reference = DotNetObjectReference.Create(new JsFunction<T>(callback))
            };
        }

        private class JsAction
        {
            private readonly Func<Task> _func;

            internal JsAction(Func<Task> func)
            {
                _func = func;
            }

            [JSInvokable]
            public async Task Invoke()
            {
                await _func.Invoke();
            }
        }

        private class JsValueAction
        {
            private readonly Func<ValueTask> _func;

            internal JsValueAction(Func<ValueTask> func)
            {
                _func = func;
            }

            [JSInvokable]
            public async ValueTask Invoke()
            {
                await _func.Invoke();
            }
        }

        private class JsFunction<T>
        {
            private readonly Func<T, Task> _func;

            internal JsFunction(Func<T, Task> func)
            {
                _func = func;
            }

            [JSInvokable]
            public async Task Invoke(T value)
            {
                await _func.Invoke(value);
            }
        }
    }
}
