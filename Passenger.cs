using System;

namespace ElevatorSimulator
{
class Passenger
{
    public int Floor { get; private set; }
    public int DestFloor { get; private set; }
    
    public Passenger()
    {
        Floor = RandomGenerator.Next(Elevator.Floor);
        DestFloor = RandomGenerator.Next(Elevator.Floor);
        while (Floor == DestFloor)
            DestFloor = RandomGenerator.Next(Elevator.Floor);

    }

    public Direction Direction {
        get {
            if (Floor > DestFloor)
                return Direction.DOWN;
            return Direction.UP;
        }
    }

    public override String ToString() {
        return string.Format("[ P{0,-9}:{1}>{2}|{3} ]", GetHashCode(), 
            Floor, DestFloor, Direction); 
    }
}
}
