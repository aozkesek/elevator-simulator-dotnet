using System;
using System.Threading;

namespace ElevatorSimulator
{
        class Program
        {
                static void Main(string[] args)
                {
                        int floor = 12;
                        int capasity = 6;
                        int speed = 2;
                        int passengerinterval = 10;
                        int runinterval = 2;

                        try {
                                floor = Int32.Parse(args[0]);
                                capasity = Int32.Parse(args[1]);
                                speed = Int32.Parse(args[2]);
                                passengerinterval = Int32.Parse(args[3]);
                                runinterval = Int32.Parse(args[4]);
                                

                        } catch(Exception e) {
                                Console.WriteLine(e);
                                return;
                        }

                        Elevator elevator = new Elevator(floor, capasity, speed);

                        Thread elevatorThread = elevator.GetNewThread();
                        Thread pgThread = PassengerGenerator.GetNewThread(elevator, passengerinterval);

                        elevatorThread.Start();
                        pgThread.Start();


                        Thread.Sleep(runinterval * 60000);
                        elevator.IsShuttingDown = true;
                        elevatorThread.Join();
                        
                }
        }
}
