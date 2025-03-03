using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProcesamientoPedidos
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Simular procesamiento de 3 pedidos en paralelo con Task
            Task[] pedidos = new Task[3];
            for (int i = 0; i < pedidos.Length; i++)
            {
                int pedidoId = i + 1;
                pedidos[i] = ProcesarPedidoAsync(pedidoId);
            }

            // Coordinación: Esperar a que se complete cualquiera de los pedidos Task.WhenAny
            Task primerPedidoCompleto = await Task.WhenAny(pedidos);
            Console.WriteLine("Se completó al menos un pedido.");

            // Esperar a que se procesen todos los pedidos en este caso no esta el task.whenall pero decidi ponerlo
            await Task.WhenAll(pedidos);
            Console.WriteLine("Todos los pedidos han sido procesados.");
        }

        static Task ProcesarPedidoAsync(int pedidoId)
        {
            // Tarea principal  Task.Run 
            return Task.Run(() =>
            {
                Console.WriteLine($"[Pedido {pedidoId}] Inicio del procesamiento.");

                // Tarea de verificación de pago con Task.Run 
                var tareaPago = Task.Run(async () =>
                {
                    Console.WriteLine($"[Pedido {pedidoId}] Verificando pago...");
                    await Task.Delay(1000); // Simula el tiempo de verificación con Task.Delay

                    // Simular error aleatorio (20% de probabilidad)
                    if(new Random().Next(0, 10) < 2)
                    {
                        throw new Exception("Error en la verificación del pago");
                    }
                    
                    Console.WriteLine($"[Pedido {pedidoId}] Pago verificado.");
                });

                // Tarea hija para actualización de inventario (adjunta a la tarea principal) con Task.Factory.StartNew
                var tareaInventario = Task.Factory.StartNew(async () =>
                {
                    Console.WriteLine($"[Pedido {pedidoId}] Actualizando inventario...");
                    await Task.Delay(800); // Simula tiempo de actualización
                    Console.WriteLine($"[Pedido {pedidoId}] Inventario actualizado.");
                }, TaskCreationOptions.AttachedToParent).Unwrap();
                // TaskCreationOptions.AttachedToParent
                // Continuación: Notificar envío si la verificación del pago fue exitosa
                tareaPago.ContinueWith((t) =>
                {
                    Console.WriteLine($"[Pedido {pedidoId}] Notificando envío...");
                }, TaskContinuationOptions.OnlyOnRanToCompletion);
                //TaskContinuationOptions.OnlyOnRanToCompletion
                // Continuación para manejo de error si la verificación del pago falla
                tareaPago.ContinueWith((t) =>
                {
                    Console.WriteLine($"[Pedido {pedidoId}] Cancelado debido a error en el pago.");
                }, TaskContinuationOptions.OnlyOnCanceled);
                //Manejo de errores TaskContinuationOptions.OnlyOnCanceled
                // Esperar a que se completen las operaciones de pago e inventario
                try
                {
                    Task.WaitAll(tareaPago, tareaInventario);
                    Console.WriteLine($"[Pedido {pedidoId}] Procesamiento completado.");
                }
                catch (AggregateException ex)
                {
                    Console.WriteLine($"[Pedido {pedidoId}] Ocurrió un error: {ex.InnerException?.Message}");
                }
            });
        }
    }
}
