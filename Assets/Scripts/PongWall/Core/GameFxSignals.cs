using System;
using UniRx;
using UnityEngine;

namespace PongWall
{
    /// <summary>Raised when the paddle sends a ball back to the wall.</summary>
    public readonly struct PaddleHitFx
    {
        public readonly Vector3 WorldPosition;
        public readonly float Speed;
        public readonly float HorizontalOffset;
        public readonly int Score;

        public PaddleHitFx(Vector3 worldPosition, float speed, float horizontalOffset, int score)
        {
            WorldPosition = worldPosition;
            Speed = speed;
            HorizontalOffset = horizontalOffset;
            Score = score;
        }
    }

    /// <summary>Raised when a ball hits the wall at the far end of the table.</summary>
    public readonly struct WallHitFx
    {
        public readonly Vector3 WorldPosition;
        public readonly float Speed;

        public WallHitFx(Vector3 worldPosition, float speed)
        {
            WorldPosition = worldPosition;
            Speed = speed;
        }
    }

    /// <summary>Raised when a ball is lost and the run ends.</summary>
    public readonly struct BallLostFx
    {
        public readonly Vector3 WorldPosition;

        public BallLostFx(Vector3 worldPosition)
        {
            WorldPosition = worldPosition;
        }
    }

    /// <summary>Raised when an extra ball is spawned.</summary>
    public readonly struct ExtraBallFx
    {
        public readonly Vector3 WorldPosition;
        public readonly int TotalBalls;

        public ExtraBallFx(Vector3 worldPosition, int totalBalls)
        {
            WorldPosition = worldPosition;
            TotalBalls = totalBalls;
        }
    }

    /// <summary>
    /// Tiny event bus for presentation-only feedback. Gameplay never depends on
    /// the listeners, so all effects can be removed without touching physics.
    /// </summary>
    public class GameFxSignals : IDisposable
    {
        public Subject<PaddleHitFx> PaddleHit { get; } = new();
        public Subject<WallHitFx> WallHit { get; } = new();
        public Subject<BallLostFx> BallLost { get; } = new();
        public Subject<ExtraBallFx> ExtraBallSpawned { get; } = new();

        public void Dispose()
        {
            PaddleHit.Dispose();
            WallHit.Dispose();
            BallLost.Dispose();
            ExtraBallSpawned.Dispose();
        }
    }
}
