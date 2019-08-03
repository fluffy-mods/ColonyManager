// Triplet.cs
// Copyright Karel Kroeze, 2017-2017

using System;
using System.Collections.Generic;

namespace FluffyManager
{
    public struct Triplet<T1, T2, T3> : IEquatable<Triplet<T1, T2, T3>>
    {
        public T1 First { get; }

        public T2 Second { get; }

        public T3 Third { get; }

        public Triplet( T1 first, T2 second, T3 third )
        {
            First  = first;
            Second = second;
            Third  = third;
        }

        public bool Equals( Triplet<T1, T2, T3> other )
        {
            return EqualityComparer<T1>.Default.Equals( First, other.First )   &&
                   EqualityComparer<T2>.Default.Equals( Second, other.Second ) &&
                   EqualityComparer<T3>.Default.Equals( Third, other.Third );
        }

        public override bool Equals( object obj )
        {
            if ( ReferenceEquals( null, obj ) ) return false;
            return obj is Triplet<T1, T2, T3> && Equals( (Triplet<T1, T2, T3>) obj );
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EqualityComparer<T1>.Default.GetHashCode( First );
                hashCode = ( hashCode * 397 ) ^ EqualityComparer<T2>.Default.GetHashCode( Second );
                hashCode = ( hashCode * 397 ) ^ EqualityComparer<T3>.Default.GetHashCode( Third );
                return hashCode;
            }
        }

        public static bool operator ==( Triplet<T1, T2, T3> left, Triplet<T1, T2, T3> right )
        {
            return left.Equals( right );
        }

        public static bool operator !=( Triplet<T1, T2, T3> left, Triplet<T1, T2, T3> right )
        {
            return !(left == right );
        }
    }
}