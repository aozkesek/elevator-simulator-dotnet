using System;
using System.Threading;

namespace ElevatorSimulator
{
        class Program
        {
                static void Main(string[] args)
                {
                        
                        Elevator elevator = new Elevator(24, 16, 2);

                        Thread elevatorThread = elevator.GetNewThread();
                        Thread pgThread = PassengerGenerator.GetNewThread(elevator, 10);

                        elevatorThread.Start();
                        pgThread.Start();


                        Thread.Sleep(3 * 60000);
                        elevator.IsShuttingDown = true;
                        elevatorThread.Join();
                        
                }
        }
}
