using System.Collections.Concurrent;
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
        private ConcurrentBag<Passenger> waitingQueue;
        private ConcurrentBag<Passenger> serviceQueue;

        public Elevator(int Capacity, int Speed) {

            this.Capacity = Capacity;
            this.Speed = Speed;

            waitingQueue = new ConcurrentBag<Passenger>();
            serviceQueue = new ConcurrentBag<Passenger>();
            
            CurrentFloor = 0;
            State = Direction.IDLE;

        }

        public bool IsAvailableRoom {
            get { return Count < Capacity; }
        }

        private int Count { get {
            return serviceQueue.Count + waitingQueue.Count;
        }}

        public bool GetOn(Passenger passenger) {
            if (Count == Capacity)
                return false;
            waitingQueue.Add(passenger);
            return true;
        }

        private void GetOff() {
            serviceQueue
                .Where(p => p.DestFloor == CurrentFloor)
                .AsParallel()
                .ForAll(p => {
                    serviceQueue.TryTake(out p);
                    Console.WriteLine("<" + p + " has got off " + this);
                });
        }

        private void GetOn() {
            waitingQueue
                .Where(p => p.Floor == CurrentFloor)
                .AsParallel()
                .ForAll(p => {
                    if (waitingQueue.TryTake(out p)) {
                        serviceQueue.Add(p);
                        Console.WriteLine(">" + p + " has got in " + this);
                    }
                });
        }

        private void LimitSpeed() {
            Thread.Sleep(Speed * 1000);
        }

        private bool IsInService 
        { get { return serviceQueue.Count > 0; } }

        private bool IsWaitingFor 
        { get { return waitingQueue.Count > 0; } }

        private void GoTo(Direction destDirection) {
            if (destDirection == Direction.UP) {
                if (CurrentFloor < Floor) {
                    LimitSpeed();
                    CurrentFloor++;
                }
            }else if (destDirection == Direction.DOWN) {
                if (CurrentFloor > 0) {
                    LimitSpeed();
                    CurrentFloor--;
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
            if (passenger.Floor < CurrentFloor)
                return Direction.DOWN;
            else if (passenger.Floor > CurrentFloor)
                return Direction.UP;
            return Direction.IDLE;
        }

        public void Report() {
        
            
        }

        private void DoService(Direction destDirection) {

                while (!IsShuttingDown && (IsWaitingFor || IsInService)) {

                        if (CurrentFloor == Floor)
                                // we are at top floor, return down
                                destDirection = Direction.DOWN;
                        else if (CurrentFloor == 0)
                                // we are at ground/basement floor
                                destDirection = Direction.UP;

                        State = destDirection;

                        Report();

                        GetOff();

                        GetOn();

                        GoTo(destDirection);

                }

                State = Direction.IDLE;
                Report();
        }

        public void Run() {

            PassengerQueue.AddElevator(this);

            while (!IsShuttingDown) {
                Thread.Yield();

                if (PassengerQueue.Count() == 0)
                        continue;

                Passenger passenger = PassengerQueue.First(this);

                if (null == passenger || !IsWaitingFor)
                        continue;

                Direction initialDirection = GetDirectionFor(passenger);
                Direction destDirection = initialDirection;

                if (destDirection == Direction.IDLE)
                        destDirection = passenger.Direction;

                DoService(destDirection);
                
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
