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
    private ConcurrentDictionary<int, Passenger> waitingQueue;
    private ConcurrentDictionary<int, Passenger> serviceQueue;

    public Elevator(int Capacity, int Speed) {

        this.Capacity = Capacity;
        this.Speed = Speed;

        waitingQueue = new ConcurrentDictionary<int, Passenger>();
        serviceQueue = new ConcurrentDictionary<int, Passenger>();
        
        CurrentFloor = 0;
        State = Direction.IDLE;

    }

    public bool IsRoomAvailable { get { return Count < Capacity; } }

    private int Count { get { return serviceQueue.Count + waitingQueue.Count; } }

    public bool WaitFor(Passenger passenger) {
        if (Count == Capacity)
            return false;
        return waitingQueue.TryAdd(passenger.GetHashCode(), passenger);            
    }

    private void GetOff() {
        serviceQueue
            .Where(p => p.Value.DestFloor == CurrentFloor)
            .AsParallel()
            .ForAll(p => {
                Passenger pr = p.Value;
                if (serviceQueue.TryRemove(pr.GetHashCode(), out pr))
                    Console.WriteLine("<" + pr + " has got off " + this);
            });
    }

    private void GetOn() {
        waitingQueue
            .Where(p => p.Value.Floor == CurrentFloor && p.Value.Direction == State)
            .AsParallel()
            .ForAll(p => {
                Passenger pr = p.Value;
                if (waitingQueue.TryRemove(pr.GetHashCode(), out pr)) {
                    if (serviceQueue.TryAdd(pr.GetHashCode(), pr))
                        Console.WriteLine(">" + pr + " has got in " + this);
                }
            });
    }

    private void LimitSpeed() {
        Thread.Sleep(Speed * 1000);
    }

    private bool IsInService { get { return serviceQueue.Count > 0; } }

    private bool IsInServiceFor(Direction direction) {
        return serviceQueue.Count(p => p.Value.Direction == direction) > 0;
    }

    private bool IsWaiting { get { return waitingQueue.Count > 0; } }

    private bool IsWaitingFor(Direction direction) {
        return waitingQueue.Count(p => {
            return (direction == Direction.UP && p.Value.Floor > CurrentFloor) 
                || (direction == Direction.DOWN && p.Value.Floor < CurrentFloor);
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

        int min = waitingQueue.Min(p => Math.Abs(CurrentFloor - p.Value.Floor));
        KeyValuePair<int, Passenger> first = waitingQueue
            .First(p => Math.Abs(CurrentFloor - p.Value.Floor) == min);
        return first.Value;
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
        return string.Format("[ E{0,-9}:{1}|{2}|{3}/{4} ]", GetHashCode(), 
            CurrentFloor, State, serviceQueue.Count, waitingQueue.Count);
    }

    public Thread GetNewThread()
    {
        return new Thread(new ThreadStart(this.Run));
    }
}
}
