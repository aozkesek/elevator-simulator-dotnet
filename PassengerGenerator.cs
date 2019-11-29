using System;
using System.Threading;

namespace ElevatorSimulator
{
sealed class PassengerGenerator
{
    private int Interval;
    
    private PassengerGenerator(int Interval)
    {
        this.Interval = Interval * 1000;
    }

    public void Run() {
        while (!Elevator.IsShuttingDown) {
            Thread.Sleep(RandomGenerator.Next(Interval));
            PassengerQueue.GetOn(new Passenger());
        }
    }

    public static Thread GetNewThread(int Interval) {
        return new Thread(new ThreadStart(new PassengerGenerator(Interval).Run));
    }
}
}
