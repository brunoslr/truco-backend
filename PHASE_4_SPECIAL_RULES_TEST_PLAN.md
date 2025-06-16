# Phase 4: Special Rules & Comprehensive Validation - Test Plan

## 📋 **Overview**
This test plan covers comprehensive validation scenarios for the truco stakes progression system, focusing on edge cases, "Mão de 10" special rules, and integration testing that wasn't covered in Phases 1-3.

## 🎯 **Test Categories**

### **Category 1: "Mão de 10" Special Rules** 🏆

#### **1.1 Basic "Mão de 10" Detection**
- [ ] **Test**: Team reaches exactly 10 points → `IsBothTeamsAt10` flag set correctly
- [ ] **Test**: Either team at 10 → hand starts with `TrucoCallState.Truco` and stakes = 4
- [ ] **Test**: Both teams at 10 → truco actions disabled, only play/fold available
- [ ] **Test**: Team goes from 9→10 during hand → next hand applies "Mão de 10" rule

#### **1.2 "Mão de 10" Integration with Truco System**
- [ ] **Test**: During "Mão de 10", calling truco should be blocked (validation)
- [ ] **Test**: During "Mão de 10", accepting non-existent truco should fail
- [ ] **Test**: During "Mão de 10", surrendering should award 4 points
- [ ] **Test**: Available actions during "Mão de 10" = `["play-card", "fold"]` only

#### **1.3 "Mão de 10" Edge Cases**
- [ ] **Test**: Team reaches 10 during active truco call → truco continues, next hand is "Mão de 10"
- [ ] **Test**: Team reaches 10 by surrendering truco → next hand is "Mão de 10"
- [ ] **Test**: Both teams reach 10 simultaneously → game ends (no "Mão de 10")
- [ ] **Test**: Team at 10 points, opponent also reaches 10 → truco disabled

### **Category 2: Complex Truco Progression Scenarios** 🎲

#### **2.1 Multi-Hand Truco Scenarios**
- [ ] **Test**: Truco → Accept → Play full hand → Next hand allows new truco
- [ ] **Test**: Truco → Raise to Seis → Accept → Hand completion and reset
- [ ] **Test**: Truco → Raise to Seis → Raise to Doze → Accept → Maximum stakes
- [ ] **Test**: Multiple hands with different truco progressions

#### **2.2 Team Alternation Complex Cases**
- [ ] **Test**: Team A calls → Team B raises → Team A raises → Team B must respond
- [ ] **Test**: Ensure consecutive calls by same team are blocked across multiple scenarios
- [ ] **Test**: Partner of caller cannot call consecutively (team-level validation)
- [ ] **Test**: After truco acceptance, either team can call new truco in next hand

#### **2.3 State Transition Edge Cases**
- [ ] **Test**: Truco call during last card of hand → proper state reset
- [ ] **Test**: Surrender during complex raise progression → proper point awarding
- [ ] **Test**: Game state consistency during rapid truco/response sequences
- [ ] **Test**: Proper cleanup when hand ends during pending truco call

### **Category 3: Integration & End-to-End Scenarios** 🔄

#### **3.1 Full Game Integration**
- [ ] **Test**: Complete game from start to finish with multiple truco calls
- [ ] **Test**: AI behavior during various truco scenarios
- [ ] **Test**: Score progression with mixed truco and regular hands
- [ ] **Test**: Game ending scenarios (team reaches 12 points via truco)

#### **3.2 API Integration Edge Cases**
- [ ] **Test**: Button press during invalid game states
- [ ] **Test**: Concurrent button presses (race conditions)
- [ ] **Test**: Network timeout during truco call/response
- [ ] **Test**: Invalid player seat during truco actions

#### **3.3 Event-Driven Architecture Validation**
- [ ] **Test**: All truco events are published correctly
- [ ] **Test**: Event ordering during complex truco sequences
- [ ] **Test**: Event handling failures and recovery
- [ ] **Test**: AI response events during truco scenarios

### **Category 4: Performance & Stress Testing** ⚡

#### **4.1 Performance Validation**
- [ ] **Test**: Multiple concurrent games with truco calls
- [ ] **Test**: Memory usage during long games with frequent truco calls
- [ ] **Test**: Response time for complex truco validation scenarios
- [ ] **Test**: Database performance with truco state persistence

#### **4.2 Stress Testing**
- [ ] **Test**: Rapid successive truco calls and responses
- [ ] **Test**: Many AI players making truco decisions simultaneously
- [ ] **Test**: Large number of action log entries during truco sequences
- [ ] **Test**: Game state size with complex truco history

### **Category 5: Error Handling & Recovery** 🛡️

#### **5.1 Validation Error Scenarios**
- [ ] **Test**: Invalid truco call during "Mão de 10"
- [ ] **Test**: Attempt to raise beyond Doze level
- [ ] **Test**: Invalid team attempting to respond to truco
- [ ] **Test**: Truco action when no pending call exists

#### **5.2 System Recovery**
- [ ] **Test**: Recovery from corrupted game state during truco
- [ ] **Test**: Proper error messages for all invalid truco scenarios
- [ ] **Test**: Transaction rollback during failed truco operations
- [ ] **Test**: Graceful handling of missing player during truco

## 🔧 **Implementation Strategy**

### **Phase 4.1: "Mão de 10" Test Suite**
1. Create `MaoDe10SpecialRulesTests.cs`
2. Implement basic detection and integration tests
3. Add edge case scenarios
4. Validate with existing endpoint tests

### **Phase 4.2: Complex Truco Scenarios**
1. Create `ComplexTrucoProgressionTests.cs`
2. Implement multi-hand and team alternation tests
3. Add state transition edge cases
4. Integration with AI behavior testing

### **Phase 4.3: Integration & Performance**
1. Enhance existing `ButtonPressEndpointTests.cs`
2. Create `TrucoIntegrationTests.cs`
3. Add performance benchmarks
4. Stress testing scenarios

### **Phase 4.4: Error Handling**
1. Create `TrucoErrorHandlingTests.cs`
2. Implement all validation scenarios
3. Test recovery mechanisms
4. Enhance error message validation

## 📊 **Success Criteria**

### **Code Coverage**
- [ ] 100% coverage of TrucoRulesEngine methods
- [ ] 100% coverage of truco-related GameStateMachine commands
- [ ] 100% coverage of "Mão de 10" logic paths
- [ ] 95% coverage of truco-related API endpoints

### **Test Quality**
- [ ] All edge cases identified and tested
- [ ] Performance benchmarks established
- [ ] Error scenarios comprehensively covered
- [ ] Integration with existing test suite

### **Business Rules Validation**
- [ ] "Mão de 10" behavior matches specification exactly
- [ ] Truco progression follows documented rules
- [ ] Team alternation enforced correctly
- [ ] Point awarding accurate in all scenarios

## 🚀 **Next Steps**

1. **Review and approve** this test plan
2. **Prioritize** test categories (suggest starting with "Mão de 10")
3. **Implement** tests incrementally with validation after each category
4. **Execute** comprehensive test suite and fix any issues found
5. **Document** any business rule clarifications needed

**Estimated Effort**: 2-3 development sessions to implement all test categories comprehensively.

Would you like me to start with a specific category, or would you like to modify/prioritize any of these test scenarios?
