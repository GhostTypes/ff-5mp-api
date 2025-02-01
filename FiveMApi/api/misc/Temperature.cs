using System;

namespace FiveMApi.api.misc
{
    public class Temperature
    {

        private readonly double _tempDouble;
        private readonly int _tempInteger;
        private readonly string _tempStr;

        public Temperature(double temp)
        {
            _tempDouble = temp;
            _tempInteger = Convert.ToInt32(temp);
            _tempStr = _tempInteger + "°C";
        }

        public double AsDouble()
        {
            return _tempDouble;
        }

        public int AsInt()
        {
            return _tempInteger;
        }

        public string AsStr()
        {
            return _tempStr;
        }


    }
}