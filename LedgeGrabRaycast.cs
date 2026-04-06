using Godot;
using System;

public partial class LedgeGrabRaycast : Node
{
	private RayCast2D _upper;
	private RayCast2D _lower;

	public override void _Ready()
	{
		_upper = GetNode<RayCast2D>("UpperRayCast");
		_lower = GetNode<RayCast2D>("LowerRayCast");
	}

	public bool CheckIsLedgeGrab()
	{
		return _lower.IsColliding() && !_upper.IsColliding();
	}
}
