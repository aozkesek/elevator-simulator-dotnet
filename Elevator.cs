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

                public static int Floor { get; set; }
                public int Capasity { get; }
                public int Speed { get; }
                public int CurrentFloor { get; private set; }
                public Direction State { get; private set; }
                public static bool IsShuttingDown { get; set; }
                public int ElevatorId { get; private set; }
                
                private WaitingPassengerQueue waitingPassengers;
                private ConcurrentDictionary<int, Passenger> passengers;
                
                public Elevator(WaitingPassengerQueue waitingPassengers, 
                        int capasity, int speed) {
                        
                        this.waitingPassengers = waitingPassengers;
                        this.Capasity = capasity;
                        this.Speed = speed;
                        
                        CurrentFloor = 0;
                        State = Direction.IDLE;
                        
                        passengers = new ConcurrentDictionary<int, Passenger>();

                }

                public bool IsAvailableRoom {
                        get { return passengers.Count < Capasity; }
                }

                public bool AddPassenger(Passenger passenger) {
                        return passengers
                                .TryAdd(passenger.GetHashCode(), passenger);
                }
                private void PickupPassengerAtCurrentFor(Direction destDirection) {
                        waitingPassengers.Pickup(this, destDirection);
                }

                private void DropPassengerAtCurrent()
                {
                        passengers.AsParallel()
                                .Where((p) => p.Value.DestFloor == CurrentFloor)
                                .ForAll((p) => {
                                        Passenger pv = p.Value;
                                        Console.WriteLine("<<< " + pv 
                                                + " is getting out from " 
                                                + ElevatorId);
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

                private bool IsSomeoneExistFor(Direction destDirection) {
                        return waitingPassengers
                                .IsPassengerWaitingFor(destDirection, CurrentFloor)
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
                                this +
                                " is going " + State +
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

                                waitingPassengers.Get(this);
                                
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
                        ElevatorId = Thread.CurrentThread.ManagedThreadId;

                        while (!IsShuttingDown) {
                                Thread.Yield();

                                if (waitingPassengers.IsEmpty)
                                        continue;

                                Passenger passenger = waitingPassengers.GetFirst(this);
                        
                                if (passenger == null || passenger.ElevatorId != ElevatorId)
                                        continue;

                                Direction initialDirection = GetDirectionFor(passenger);
                                Direction destDirection = initialDirection;

                                if (destDirection == Direction.IDLE)
                                        destDirection = passenger.Direction;

                                DoService(destDirection);

                        }
                }

                public override String ToString() {
                        return "Elevator-" + ElevatorId;
                }

                public Thread GetNewThread() 
                {
                        return new Thread(new ThreadStart(this.Run));
                }
        }
}
