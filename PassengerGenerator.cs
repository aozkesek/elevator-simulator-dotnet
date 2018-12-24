using System;
using System.Threading;

namespace ElevatorSimulator
{
        class PassengerGenerator
        {
                private Elevator elevator;
                private int Interval;
                
                public PassengerGenerator(Elevator elevator, int interval)
                {
                        this.elevator = elevator;
                        this.Interval = interval * 1000;
                }

                public void Run() {
                        Random random = new Random();

                        while (!elevator.IsShuttingDown) {
                                Thread.Sleep(random.Next(Interval));
                                new Passenger(elevator);
                        }
                }

                public static Thread GetNewThread(
                        Elevator elevator, int interval) {

                        return new Thread(new ThreadStart(
                                new PassengerGenerator(elevator, interval).Run));
                }
        }
}
