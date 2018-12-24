using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System;

namespace ElevatorSimulator
{
        enum Direction { UP, DOWN, IDLE }

        class Elevator
        {

                public int Floor { get; }
                public int Capasity { get; }
                public int Speed { get; }
                public int CurrentFloor { get; private set; }
                public Direction State { get; private set; }
                public bool IsShuttingDown { get; set; }
                
                private ConcurrentDictionary<int, Passenger> waitingPassengers;
                private ConcurrentDictionary<int, Passenger> passengers;
                
                public Elevator(int floor, int capasity, int speed) {
                        this.Floor = floor;
                        this.Capasity = capasity;
                        this.Speed = speed;

                        CurrentFloor = 0;
                        State = Direction.IDLE;
                        IsShuttingDown = false;

                        waitingPassengers = new ConcurrentDictionary<int, Passenger>();
                        passengers = new ConcurrentDictionary<int, Passenger>();

                }

                public void QueuePassenger(Passenger passenger) {
                        Console.WriteLine(passenger + " is queued.");
                        waitingPassengers.TryAdd(passenger.GetHashCode(), passenger);
                }

                private void PickupPassengerAtCurrentFor(Direction destDirection) {
                        waitingPassengers.AsParallel()
                                .Where((p) => p.Value.Floor == CurrentFloor && p.Value.Direction == destDirection )
                                .ForAll((p) => {
                                        if (passengers.Count < Capasity) {
                                                Passenger pv = p.Value;
                                                Console.WriteLine(">>> " + pv + " is getting in.");
                                                passengers.TryAdd(p.Key, p.Value);
                                                waitingPassengers.TryRemove(p.Key, out pv);    
                                        }
                                        
                                });
                }

                private void DropPassengerAtCurrent()
                {
                        passengers.AsParallel()
                                .Where((p) => p.Value.DestFloor == CurrentFloor)
                                .ForAll((p) => {
                                        Passenger pv = p.Value;
                                        Console.WriteLine("<<< " + pv + " is getting out.");
                                        passengers.TryRemove(p.Key, out pv);
                                });
                }

                private void GoUp()
                {
                        if (!IsSomeoneWaiting)
                                return;

                        State = Direction.UP;
                        if (CurrentFloor < Floor) {
                                LimitSpeed();
                                CurrentFloor++;
                        } else
                                State = Direction.IDLE;

                }

                private void GoDown()
                {
                        if (!IsSomeoneWaiting)
                                return;
                        
                        State = Direction.DOWN;
                        if (CurrentFloor > 0) {
                                LimitSpeed();
                                CurrentFloor--;
                        } else
                                State = Direction.IDLE;

                }

                private void LimitSpeed() {
                        Thread.Sleep(Speed * 1000);
                }
                
                private bool IsSomeoneWaiting {
                        get {
                                if (waitingPassengers.IsEmpty && passengers.IsEmpty) {
                                        State = Direction.IDLE;
                                        return false;
                                }

                                return true;
                        }
                }

                private bool IsWaitingFor(Passenger passenger, Direction destDirection) {
                        return passenger.Floor < CurrentFloor && destDirection == Direction.DOWN ||
                                passenger.Floor > CurrentFloor && destDirection == Direction.UP;
                }
                private bool IsSomeoneExistFor(Direction destDirection) {
                        return waitingPassengers.AsParallel()
                                        .Where(p => IsWaitingFor(p.Value, destDirection))
                                        .Count() > 0
                                || !passengers.IsEmpty;
                }

                private void GoTo(Direction destDirection) {
                        if (destDirection == Direction.UP)
                                GoUp();
                        else if (destDirection == Direction.DOWN)
                                GoDown();
                }

                private Direction SwapDirection(Direction destDirection) {
                        if (destDirection == Direction.UP)
                                return Direction.DOWN;
                        else if (destDirection == Direction.DOWN)
                                return Direction.UP;
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
                        Console.WriteLine(
                                "Elevator is going " + State +
                                " currently at " + CurrentFloor +
                                ", total " + waitingPassengers.Count + " person are waiting" +
                                ", total " + passengers.Count + " person are giving service"
                        );
                } 

                private void DoService(Direction destDirection) {

                        while (!IsShuttingDown && IsSomeoneWaiting) {

                                if (CurrentFloor == Floor) 
                                        // we are at top floor, return down
                                        destDirection = Direction.DOWN;
                                else if (CurrentFloor == 0)
                                        // we are at ground/basement floor
                                        destDirection = Direction.UP;

                                State = destDirection;
                                
                                Report();

                                DropPassengerAtCurrent();

                                PickupPassengerAtCurrentFor(destDirection);
                                if (!IsSomeoneExistFor(destDirection)) {
                                        destDirection = SwapDirection(destDirection);
                                        PickupPassengerAtCurrentFor(destDirection);
                                }

                                GoTo(destDirection);

                        }

                        Report();
                }

                public void Run() {

                        while (!IsShuttingDown) {
                                Thread.Yield();

                                if (waitingPassengers.IsEmpty)
                                        continue;

                                Passenger passenger = waitingPassengers.First().Value;
                                Direction initialDirection = GetDirectionFor(passenger);
                                Direction destDirection = initialDirection;

                                if (destDirection == Direction.IDLE)
                                        destDirection = passenger.Direction;

                                Report();

                                DoService(destDirection);

                        }
                }

                public Thread GetNewThread() 
                {
                        return new Thread(new ThreadStart(this.Run));
                }
        }
}
