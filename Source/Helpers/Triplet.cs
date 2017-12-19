// Triplet.cs
// Copyright Karel Kroeze, 2017-2017

using System;
using System.Collections.Generic;

namespace FluffyManager
{
    public struct Triplet<T1, T2, T3>: IEquatable<Triplet<T1, T2, T3>>
    {
        private T1 first;
        private T2 second;
        private T3 third;

        public T1 First => first;
        public T2 Second => second;
        public T3 Third => third;

        public Triplet( T1 first, T2 second, T3 third )
        {
            this.first = first;
            this.second = second;
            this.third = third;
        }

        public bool Equals( Triplet<T1, T2, T3> other )
        {
            return EqualityComparer<T1>.Default.Equals( first, other.first ) && EqualityComparer<T2>.Default.Equals( second, other.second ) && EqualityComparer<T3>.Default.Equals( third, other.third );
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
                var hashCode = EqualityComparer<T1>.Default.GetHashCode( first );
                hashCode = ( hashCode * 397 ) ^ EqualityComparer<T2>.Default.GetHashCode( second );
                hashCode = ( hashCode * 397 ) ^ EqualityComparer<T3>.Default.GetHashCode( third );
                return hashCode;
            }
        }

        public static bool operator ==( Triplet<T1, T2, T3> left, Triplet<T1, T2, T3> right )
        {
            return left.Equals( right );
        }

        public static bool operator !=( Triplet<T1, T2, T3> left, Triplet<T1, T2, T3> right )
        {
            return !left.Equals( right );
        }
    }
}