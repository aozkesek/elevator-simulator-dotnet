using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System;

namespace ElevatorSimulator
{
enum Direction { UP, DOWN, IDLE }

class Elevator
{

    public static int Floor { get; set; }
    public static bool IsShuttingDown { get; set; }
    
    public int Capacity { get; }
    public int Speed { get; }

    private int _currentFloor;
    private Object lck = new Object();
    public int CurrentFloor { 
        get { lock(lck) { return _currentFloor; }} 
        private set { lock(lck) { _currentFloor = value; }} 
    }

    public Direction State { get; private set; }
    private BlockingCollection<Passenger> waitingQueue;
    private BlockingCollection<Passenger> serviceQueue;

    public Elevator(int Capacity, int Speed) {

        this.Capacity = Capacity;
        this.Speed = Speed;

        waitingQueue = new BlockingCollection<Passenger>();
        serviceQueue = new BlockingCollection<Passenger>();
        
        CurrentFloor = 0;
        State = Direction.IDLE;

    }

    public bool IsRoomAvailable { get { return Count < Capacity; } }

    private int Count { get { return serviceQueue.Count + waitingQueue.Count; } }

    public bool WaitFor(Passenger passenger) {
        if (Count == Capacity)
            return false;
        waitingQueue.Add(passenger);
        return true;
    }

    private void GetOff() {
        serviceQueue
            .TakeWhile(p => p.DestFloor == CurrentFloor)
            .AsParallel()
            .ForAll(p => Console.WriteLine("<" + p + " has got off " + this));
    }

    private void GetOn() {
        waitingQueue
            .TakeWhile((p, i) => p.Floor == CurrentFloor && p.Direction == State)
            .AsParallel()
            .ForAll(p => {
                serviceQueue.Add(p);
                Console.WriteLine(">" + p + " has got in " + this);
            });
    }

    private void LimitSpeed() {
        Thread.Sleep(Speed * 1000);
    }

    private bool IsInService { get { return serviceQueue.Count > 0; } }

    private bool IsInServiceFor(Direction direction) {
        return serviceQueue.Count(p => p.Direction == direction) > 0;
    }

    private bool IsWaiting { get { return waitingQueue.Count > 0; } }

    private bool IsWaitingFor(Direction direction) {
        return waitingQueue.Count(p => {
            return (direction == Direction.UP && p.Floor > CurrentFloor) 
                || (direction == Direction.DOWN && p.Floor < CurrentFloor);
        }) > 0;
    }

    private void GoTo() {
        LimitSpeed();
        if (State == Direction.UP) {
            if (CurrentFloor < Floor) {
                CurrentFloor++;
            } else {
                State = Direction.DOWN;
                CurrentFloor--;
            }
        } else if (State == Direction.DOWN) {
            if (CurrentFloor > 0) {
                CurrentFloor--;
            } else {
                CurrentFloor++;
            }
        }
    }

    private Direction SwapDirection(Direction destDirection) {
            switch(destDirection) {
                    case Direction.UP: return Direction.DOWN;
                    case Direction.DOWN: return Direction.UP;
            }
            return Direction.IDLE;
    }

    private Direction GetDirectionFor(Passenger passenger) {
        if (null == passenger)
            return Direction.IDLE;

        if (passenger.Floor < CurrentFloor)
            return Direction.DOWN;
        else if (passenger.Floor > CurrentFloor)
            return Direction.UP;
        return passenger.Direction;
    }

    public void Report() {
        Console.WriteLine("*" + this);        
    }

    private void DoService() {

            while (!IsShuttingDown && State != Direction.IDLE && (IsWaiting || IsInService)) {

                    Report();

                    GetOff();

                    GetOn();

                    if (!IsInServiceFor(State) && !IsWaitingFor(State)) {
                        State = SwapDirection(State);
                        GetOn();
                    }
                        
                    GoTo();

            }

            State = Direction.IDLE;
            Report();
    }

    private Passenger Nearest() {
        if (!IsWaiting)
            return null;

        int min = waitingQueue.Min(p => Math.Abs(CurrentFloor - p.Floor));
        return waitingQueue.First(p => Math.Abs(CurrentFloor - p.Floor) == min);
    }

    public void Run() {

        PassengerQueue.AddElevator(this);

        while (!IsShuttingDown) {
            Thread.Yield();

            if (!IsWaiting)
                continue;

            State = GetDirectionFor(Nearest());
            
            DoService();
            
        }

        PassengerQueue.RemoveElevator(this);
    }

    public override String ToString() {
        return string.Format("[ E{0,-9}: {1}|{2}|{3}/{4} ]", GetHashCode(), 
            CurrentFloor, State, serviceQueue.Count, waitingQueue.Count);
    }

    public Thread GetNewThread()
    {
        return new Thread(new ThreadStart(this.Run));
    }
}
}
