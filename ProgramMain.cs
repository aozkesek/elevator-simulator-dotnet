using System;
using System.Threading;
using System.Threading.Tasks;

namespace ElevatorSimulator
{
        public sealed class ProgramMain
        {
                public static void Run(string[] args)
                {
                        Elevator.Floor = 12;
                        Elevator.IsShuttingDown = false;

                        int capasity = 6;
                        int speed = 2;
                        int passengerinterval = 3;
                        int runinterval = 1;
                        int elevatorcount = 4;

                        try {
                                if (args.Length > 0)
                                        Elevator.Floor = Int32.Parse(args[0]);
                                if (args.Length > 1)
                                        capasity = Int32.Parse(args[1]);
                                if (args.Length > 2)
                                        speed = Int32.Parse(args[2]);
                                if (args.Length > 3)
                                        elevatorcount = Int32.Parse(args[3]);
                                if (args.Length > 4)
                                        passengerinterval = Int32.Parse(args[4]);
                                if (args.Length > 5)
                                        runinterval = Int32.Parse(args[5]);
                                

                        } catch(Exception e) {
                                Console.WriteLine(e);
                                return;
                        }

                        Parallel.For (0, elevatorcount, i =>
                                ThreadPool.QueueUserWorkItem(ElevatorThreadProc,
                                        new Elevator(capasity, speed)));

                        Thread passgen = PassengerGenerator.GetNewThread(passengerinterval);
                                
                        passgen.Start();

                        Thread.Sleep(runinterval * 60000);
                        
                        Elevator.IsShuttingDown = true;

                        passgen.Join();
                 
                }

                public static void ElevatorThreadProc(Object elevator) {

                        if (elevator == null || !(elevator is Elevator))
                                return;

                        Thread elevatorthread = ((Elevator)elevator).GetNewThread();
                        elevatorthread.Start();
                        elevatorthread.Join();

                }
        }
}
