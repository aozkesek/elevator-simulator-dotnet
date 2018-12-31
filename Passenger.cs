using System;
using System.Threading;

namespace ElevatorSimulator
{
        class Passenger
        {

                private static Random random = new Random();

                public int Floor { get; private set; }
                public int DestFloor { get; private set; }
                private Object lockobject = new Object();
                private int elevatorid;
                public int ElevatorId { 
                        get { return this.elevatorid; } 
                        set { 
                                Monitor.Enter(this.lockobject);
                                if (this.elevatorid == -1) 
                                        this.elevatorid = value; 
                                Monitor.Exit(this.lockobject);
                                } 
                        }
                
                public Passenger()
                {
                        elevatorid = -1;
                        Floor = random.Next(Elevator.Floor);
                        DestFloor = random.Next(Elevator.Floor);
                        while (Floor == DestFloor)
                                DestFloor = random.Next(Elevator.Floor);

                }

                public Direction Direction {
                        get {
                                if (Floor > DestFloor)
                                        return Direction.DOWN;
                                else if (Floor < DestFloor)
                                        return Direction.UP;
                                
                                return Direction.IDLE;
                        }
                }

                public override String ToString() {
                        return "<Passenger: at " + Floor + " to " + DestFloor + ">"; 
                }
        }
}
