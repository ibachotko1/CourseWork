using System;
using System.Collections.Generic;
using System.Linq;
using SmartGreenhouse.Core.WpCalculator;
using WpCalc = SmartGreenhouse.Core.WpCalculator.WpCalculator;

namespace SmartGreenhouse.Core.Invariants
{
    public enum LoopMode
    {
        PrefixSum,
        CountGreaterThanT,
        PrefixMax
    }

    public class LoopData
    {
        public List<double> Values { get; set; } = new List<double>();
        public int CurrentIndex { get; set; } = 0;
        public double Result { get; set; } = 0;
        public double Threshold { get; set; } = 0;
        public LoopMode Mode { get; set; }
    }

    public class InvariantInfo
    {
        public string InvariantDescription { get; set; } = "";
        public string InvariantFormula { get; set; } = "";
        public string VariantFunction { get; set; } = "";
        public int VariantValue { get; set; } = 0;
        public bool InvariantBeforeStep { get; set; } = false;
        public bool InvariantAfterStep { get; set; } = false;
    }

    public class LoopInvariantChecker
    {
        private LoopData _data;
        private InvariantInfo _invariant;
        private string _preCondition = "";
        private string _postCondition = "";
        private string _loopBody = "";

        public LoopInvariantChecker(LoopData data, InvariantInfo invariant)
        {
            _data = data;
            _invariant = invariant;
            InitializeConditions();
        }

        private void InitializeConditions()
        {
            switch (_data.Mode)
            {
                case LoopMode.PrefixSum:
                    _preCondition = "res == 0 && j == 0";
                    _postCondition = $"res == sum(temperature[0..n-1])";
                    _loopBody = "res := res + temperature[j]; j := j + 1";
                    break;
                case LoopMode.CountGreaterThanT:
                    _preCondition = "res == 0 && j == 0";
                    _postCondition = "res == count(temperature[0..n-1] < 15)";
                    _loopBody = "if (temperature[j] < 15) { res := res + 1 } else { }; j := j + 1";
                    break;
                case LoopMode.PrefixMax:
                    _preCondition = "j == 0 && res == temperature[0]";
                    _postCondition = $"res == max(temperature[0..n-1])";
                    _loopBody = "if (temperature[j] > res) { res := temperature[j] } else { }; j := j + 1";
                    break;
            }
        }

        public string GetPreCondition() => _preCondition;
        public string GetPostCondition() => _postCondition;
        public string GetLoopBody() => _loopBody;

        public VerificationConditions CheckVerificationConditions()
        {
            var vc = new VerificationConditions();

            vc.PreImpliesInv = CheckPreImpliesInv();
            vc.InvariantPreservation = CheckInvariantPreservation();
            vc.ExitImpliesPost = CheckExitImpliesPost();
            vc.VariantDecreases = CheckVariantDecreases();

            return vc;
        }

        private string CheckPreImpliesInv()
        {
            return $"Pre ⇒ Inv: {_preCondition} ⇒ {_invariant.InvariantFormula}";
        }

        private string CheckInvariantPreservation()
        {
            string wpBody = CalculateWpForBody();
            return $"(Inv ∧ B) ⇒ wp(S, Inv): ({_invariant.InvariantFormula} ∧ j < n) ⇒ {wpBody}";
        }

        private string CheckExitImpliesPost()
        {
            return $"(Inv ∧ ¬B) ⇒ Post: ({_invariant.InvariantFormula} ∧ j >= n) ⇒ {_postCondition}";
        }

        private string CheckVariantDecreases()
        {
            string wpVariant = CalculateWpForVariant();
            return $"Inv ∧ B ⇒ (t' < t): ({_invariant.InvariantFormula} ∧ j < n) ⇒ {wpVariant} < {_invariant.VariantFunction}";
        }

        private string CalculateWpForBody()
        {
            var fragment = new WpFragment
            {
                PostCondition = _invariant.InvariantFormula,
                Code = _loopBody
            };

            var result = WpCalc.CalculateWp(fragment);
            return result.FinalWp;
        }

        private string CalculateWpForVariant()
        {
            string variantAfter = _invariant.VariantFunction.Replace("j", "j + 1");
            var fragment = new WpFragment
            {
                PostCondition = $"t' == {variantAfter}",
                Code = _loopBody
            };

            var result = WpCalc.CalculateWp(fragment);
            return result.FinalWp;
        }

        public bool ExecuteStep()
        {
            if (_data.CurrentIndex >= _data.Values.Count)
                return false;

            _invariant.InvariantBeforeStep = CheckInvariant();

            switch (_data.Mode)
            {
                case LoopMode.PrefixSum:
                    _data.Result += _data.Values[_data.CurrentIndex];
                    break;
                case LoopMode.CountGreaterThanT:
                    if (_data.Values[_data.CurrentIndex] < 15.0)
                        _data.Result += 1;
                    break;
                case LoopMode.PrefixMax:
                    if (_data.CurrentIndex >= _data.Values.Count)
                        break;
                    if (_data.CurrentIndex == 0)
                    {
                        _data.Result = _data.Values[0];
                    }
                    else if (_data.CurrentIndex < _data.Values.Count)
                    {
                        _data.Result = Math.Max(_data.Result, _data.Values[_data.CurrentIndex]);
                    }
                    break;
            }

            _data.CurrentIndex++;
            UpdateVariant();
            _invariant.InvariantAfterStep = CheckInvariant();

            return _data.CurrentIndex < _data.Values.Count;
        }

        private bool CheckInvariant()
        {
            switch (_data.Mode)
            {
                case LoopMode.PrefixSum:
                    if (_data.CurrentIndex == 0)
                        return Math.Abs(_data.Result - 0) < 0.001;
                    double expectedSum = _data.Values.Take(_data.CurrentIndex).Sum();
                    bool sumCorrect = Math.Abs(_data.Result - expectedSum) < 0.001;
                    bool indexValid = _data.CurrentIndex >= 0 && _data.CurrentIndex <= _data.Values.Count;
                    return sumCorrect && indexValid;
                case LoopMode.CountGreaterThanT:
                    int expectedCount = _data.Values.Take(_data.CurrentIndex).Count(v => v < 15.0);
                    bool countCorrect = Math.Abs(_data.Result - expectedCount) < 0.001;
                    bool indexValid2 = _data.CurrentIndex >= 0 && _data.CurrentIndex <= _data.Values.Count;
                    return countCorrect && indexValid2;
                case LoopMode.PrefixMax:
                    if (_data.CurrentIndex == 0)
                        return _data.Values.Count > 0 && Math.Abs(_data.Result - _data.Values[0]) < 0.001;
                    if (_data.CurrentIndex > _data.Values.Count)
                        return false;
                    var prefix = _data.Values.Take(_data.CurrentIndex);
                    if (!prefix.Any()) return false;
                    double expectedMax = prefix.Max();
                    bool maxCorrect = Math.Abs(_data.Result - expectedMax) < 0.001;
                    bool indexValid3 = _data.CurrentIndex >= 0 && _data.CurrentIndex <= _data.Values.Count;
                    return maxCorrect && indexValid3;
                default:
                    return false;
            }
        }

        private void UpdateVariant()
        {
            _invariant.VariantValue = _data.Values.Count - _data.CurrentIndex;
        }

        public void Reset()
        {
            _data.CurrentIndex = 0;
            _data.Result = 0;
            if (_data.Mode == LoopMode.PrefixMax && _data.Values.Count > 0)
            {
                _data.Result = _data.Values[0];
            }
            _invariant.VariantValue = _data.Values.Count;
            _invariant.InvariantBeforeStep = false;
            _invariant.InvariantAfterStep = false;
        }
    }

    public class VerificationConditions
    {
        public string PreImpliesInv { get; set; } = "";
        public string InvariantPreservation { get; set; } = "";
        public string ExitImpliesPost { get; set; } = "";
        public string VariantDecreases { get; set; } = "";
    }
}

