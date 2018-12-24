using System;

namespace ElevatorSimulator
{
        class Passenger
        {

                private static Random random = new Random();

                public int Floor { get; private set; }
                public int DestFloor { get; private set; }
                
                public Passenger(Elevator elevator)
                {
                        Floor = random.Next(elevator.Floor);
                        DestFloor = random.Next(elevator.Floor);
                        while (Floor == DestFloor)
                                DestFloor = random.Next(elevator.Floor);

                        elevator.QueuePassenger(this);
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
