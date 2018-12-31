using System;
using System.Threading;

namespace ElevatorSimulator
{
        class PassengerGenerator
        {
                private int Interval;
                private WaitingPassengerQueue waitingPassengers;
                
                public PassengerGenerator(int interval, 
                        WaitingPassengerQueue waitingPassengers)
                {
                        this.Interval = interval * 1000;
                        this.waitingPassengers = waitingPassengers;
                }

                public void Run() {
                        Random random = new Random();

                        while (!Elevator.IsShuttingDown) {
                                Thread.Sleep(random.Next(Interval));
                                waitingPassengers.Add(new Passenger());
                        }
                }

                public static Thread GetNewThread(int interval,
                        WaitingPassengerQueue waitingPassengers) {

                        return new Thread(new ThreadStart(
                                new PassengerGenerator(interval, waitingPassengers)
                                        .Run));
                }
        }
}
