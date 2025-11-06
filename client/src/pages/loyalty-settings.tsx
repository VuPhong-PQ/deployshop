import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiRequest } from "../lib/utils";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Badge } from "@/components/ui/badge";
import { Gift, Star, Clock, Calendar, Calculator, Users, Award, TrendingUp } from "lucide-react";

export type LoyaltyConfig = {
  loyaltyConfigId?: number;
  isEnabled: boolean;
  pointsPerCurrency: number;
  minOrderAmountForPoints: number;
  maxPointsPerOrder?: number;
  pointExpiryDays: number;
  allowPointRedemption: boolean;
  pointValue: number;
  maxRedemptionPercentage: number;
  
  // Time-based bonuses
  happyHourEnabled: boolean;
  happyHourStartTime: string;
  happyHourEndTime: string;
  happyHourMultiplier: number;
  weekendBonusEnabled: boolean;
  weekendMultiplier: number;
  birthdayBonusEnabled: boolean;
  birthdayMultiplier: number;
  birthdayValidDays: number;
};

export type CustomerTier = {
  tierId?: number;
  tierName: string;
  minSpent: number;
  minPoints: number;
  pointsMultiplier: number;
  discountPercentage: number;
  description?: string;
  tierColor: string;
  isActive: boolean;
};

export function LoyaltySettings() {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState("general");

  // Get current loyalty config
  const { data: config, isLoading } = useQuery<LoyaltyConfig | null>({
    queryKey: ["/api/LoyaltyConfig"],
    queryFn: async () => {
      const res = await apiRequest("/api/LoyaltyConfig", { method: "GET" });
      return res;
    },
  });

  // Get customer tiers
  const { data: tiers = [] } = useQuery<CustomerTier[]>({
    queryKey: ["/api/CustomerTiers"],
    queryFn: async () => {
      const res = await apiRequest("/api/CustomerTiers", { method: "GET" });
      return res || [];
    },
  });

  const [form, setForm] = useState<LoyaltyConfig>({
    isEnabled: true,
    pointsPerCurrency: 1000,
    minOrderAmountForPoints: 50000,
    pointExpiryDays: 365,
    allowPointRedemption: true,
    pointValue: 1000,
    maxRedemptionPercentage: 50,
    happyHourEnabled: false,
    happyHourStartTime: "17:00",
    happyHourEndTime: "19:00",
    happyHourMultiplier: 2.0,
    weekendBonusEnabled: false,
    weekendMultiplier: 1.5,
    birthdayBonusEnabled: false,
    birthdayMultiplier: 3.0,
    birthdayValidDays: 7,
  });

  const [testAmount, setTestAmount] = useState(100000);
  const [calculationResult, setCalculationResult] = useState<any>(null);

  useEffect(() => {
    if (config) {
      setForm({
        ...config,
        happyHourStartTime: config.happyHourStartTime || "17:00",
        happyHourEndTime: config.happyHourEndTime || "19:00",
      });
    }
  }, [config]);

  const mutation = useMutation({
    mutationFn: async (data: LoyaltyConfig) => {
      const res = await apiRequest("/api/LoyaltyConfig", { 
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data)
      });
      return res;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["/api/LoyaltyConfig"] });
      alert("Đã lưu cấu hình tích điểm!");
    },
  });

  const calculatePointsMutation = useMutation({
    mutationFn: async (amount: number) => {
      const res = await apiRequest(`/api/LoyaltyConfig/calculate-points?amount=${amount}`, { 
        method: "GET"
      });
      return res;
    },
    onSuccess: (data) => {
      setCalculationResult(data);
    },
  });

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const { name, value, type, checked } = e.target;
    setForm(prev => ({
      ...prev,
      [name]: type === "checkbox" ? checked : (type === "number" ? parseFloat(value) || 0 : value)
    }));
  }

  function handleSwitchChange(name: string, checked: boolean) {
    setForm(prev => ({ ...prev, [name]: checked }));
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    mutation.mutate(form);
  }

  function calculateTestPoints() {
    calculatePointsMutation.mutate(testAmount);
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-2 text-gray-600">Đang tải cấu hình...</p>
        </div>
      </div>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Gift className="w-6 h-6 text-purple-600" />
          Cài đặt Tích điểm thưởng
        </CardTitle>
      </CardHeader>
      <CardContent>
        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <TabsList className="grid w-full grid-cols-4">
            <TabsTrigger value="general" className="flex items-center gap-2">
              <Star className="w-4 h-4" />
              Cài đặt chung
            </TabsTrigger>
            <TabsTrigger value="bonuses" className="flex items-center gap-2">
              <Clock className="w-4 h-4" />
              Bonus thời gian
            </TabsTrigger>
            <TabsTrigger value="tiers" className="flex items-center gap-2">
              <Award className="w-4 h-4" />
              Cấp độ khách hàng
            </TabsTrigger>
            <TabsTrigger value="calculator" className="flex items-center gap-2">
              <Calculator className="w-4 h-4" />
              Tính điểm
            </TabsTrigger>
          </TabsList>

          <form onSubmit={handleSubmit} className="mt-6">
            <TabsContent value="general" className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <Label htmlFor="isEnabled" className="text-base font-medium">
                      Kích hoạt tích điểm
                    </Label>
                    <Switch
                      id="isEnabled"
                      checked={form.isEnabled}
                      onCheckedChange={(checked) => handleSwitchChange("isEnabled", checked)}
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="pointsPerCurrency">Tỷ lệ tích điểm (VND/điểm)</Label>
                    <Input
                      id="pointsPerCurrency"
                      name="pointsPerCurrency"
                      type="number"
                      value={form.pointsPerCurrency}
                      onChange={handleChange}
                      placeholder="1000"
                    />
                    <p className="text-sm text-gray-500">
                      Ví dụ: 1000 = Mỗi 1.000 VND được 1 điểm
                    </p>
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="minOrderAmountForPoints">Đơn hàng tối thiểu (VND)</Label>
                    <Input
                      id="minOrderAmountForPoints"
                      name="minOrderAmountForPoints"
                      type="number"
                      value={form.minOrderAmountForPoints}
                      onChange={handleChange}
                      placeholder="50000"
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="maxPointsPerOrder">Điểm tối đa/đơn hàng</Label>
                    <Input
                      id="maxPointsPerOrder"
                      name="maxPointsPerOrder"
                      type="number"
                      value={form.maxPointsPerOrder || ""}
                      onChange={handleChange}
                      placeholder="Không giới hạn"
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="pointExpiryDays">Điểm hết hạn sau (ngày)</Label>
                    <Input
                      id="pointExpiryDays"
                      name="pointExpiryDays"
                      type="number"
                      value={form.pointExpiryDays}
                      onChange={handleChange}
                      placeholder="365"
                    />
                  </div>
                </div>

                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <Label htmlFor="allowPointRedemption" className="text-base font-medium">
                      Cho phép đổi điểm
                    </Label>
                    <Switch
                      id="allowPointRedemption"
                      checked={form.allowPointRedemption}
                      onCheckedChange={(checked) => handleSwitchChange("allowPointRedemption", checked)}
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="pointValue">Giá trị điểm (VND/điểm)</Label>
                    <Input
                      id="pointValue"
                      name="pointValue"
                      type="number"
                      value={form.pointValue}
                      onChange={handleChange}
                      placeholder="1000"
                    />
                    <p className="text-sm text-gray-500">
                      Ví dụ: 1000 = 1 điểm = 1.000 VND
                    </p>
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="maxRedemptionPercentage">Tối đa đổi điểm (%)</Label>
                    <Input
                      id="maxRedemptionPercentage"
                      name="maxRedemptionPercentage"
                      type="number"
                      value={form.maxRedemptionPercentage}
                      onChange={handleChange}
                      placeholder="50"
                    />
                    <p className="text-sm text-gray-500">
                      Tối đa % hóa đơn có thể thanh toán bằng điểm
                    </p>
                  </div>
                </div>
              </div>
            </TabsContent>

            <TabsContent value="bonuses" className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <Card>
                  <CardHeader className="pb-3">
                    <CardTitle className="text-lg flex items-center gap-2">
                      <Clock className="w-5 h-5 text-orange-600" />
                      Giờ vàng (Happy Hour)
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="flex items-center justify-between">
                      <Label>Kích hoạt</Label>
                      <Switch
                        checked={form.happyHourEnabled}
                        onCheckedChange={(checked) => handleSwitchChange("happyHourEnabled", checked)}
                      />
                    </div>

                    {form.happyHourEnabled && (
                      <>
                        <div className="grid grid-cols-2 gap-4">
                          <div>
                            <Label htmlFor="happyHourStartTime">Từ</Label>
                            <Input
                              id="happyHourStartTime"
                              name="happyHourStartTime"
                              type="time"
                              value={form.happyHourStartTime}
                              onChange={handleChange}
                            />
                          </div>
                          <div>
                            <Label htmlFor="happyHourEndTime">Đến</Label>
                            <Input
                              id="happyHourEndTime"
                              name="happyHourEndTime"
                              type="time"
                              value={form.happyHourEndTime}
                              onChange={handleChange}
                            />
                          </div>
                        </div>

                        <div>
                          <Label htmlFor="happyHourMultiplier">Hệ số nhân</Label>
                          <Input
                            id="happyHourMultiplier"
                            name="happyHourMultiplier"
                            type="number"
                            step="0.1"
                            value={form.happyHourMultiplier}
                            onChange={handleChange}
                          />
                        </div>
                      </>
                    )}
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader className="pb-3">
                    <CardTitle className="text-lg flex items-center gap-2">
                      <Calendar className="w-5 h-5 text-blue-600" />
                      Cuối tuần
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="flex items-center justify-between">
                      <Label>Kích hoạt</Label>
                      <Switch
                        checked={form.weekendBonusEnabled}
                        onCheckedChange={(checked) => handleSwitchChange("weekendBonusEnabled", checked)}
                      />
                    </div>

                    {form.weekendBonusEnabled && (
                      <div>
                        <Label htmlFor="weekendMultiplier">Hệ số nhân</Label>
                        <Input
                          id="weekendMultiplier"
                          name="weekendMultiplier"
                          type="number"
                          step="0.1"
                          value={form.weekendMultiplier}
                          onChange={handleChange}
                        />
                      </div>
                    )}
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader className="pb-3">
                    <CardTitle className="text-lg flex items-center gap-2">
                      <Gift className="w-5 h-5 text-pink-600" />
                      Sinh nhật
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="flex items-center justify-between">
                      <Label>Kích hoạt</Label>
                      <Switch
                        checked={form.birthdayBonusEnabled}
                        onCheckedChange={(checked) => handleSwitchChange("birthdayBonusEnabled", checked)}
                      />
                    </div>

                    {form.birthdayBonusEnabled && (
                      <>
                        <div>
                          <Label htmlFor="birthdayMultiplier">Hệ số nhân</Label>
                          <Input
                            id="birthdayMultiplier"
                            name="birthdayMultiplier"
                            type="number"
                            step="0.1"
                            value={form.birthdayMultiplier}
                            onChange={handleChange}
                          />
                        </div>

                        <div>
                          <Label htmlFor="birthdayValidDays">Hiệu lực (ngày)</Label>
                          <Input
                            id="birthdayValidDays"
                            name="birthdayValidDays"
                            type="number"
                            value={form.birthdayValidDays}
                            onChange={handleChange}
                          />
                          <p className="text-sm text-gray-500 mt-1">
                            Áp dụng trong vòng bao nhiêu ngày quanh sinh nhật
                          </p>
                        </div>
                      </>
                    )}
                  </CardContent>
                </Card>
              </div>
            </TabsContent>

            <TabsContent value="tiers" className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                {tiers.map((tier) => (
                  <Card key={tier.tierId} className="border-2" style={{ borderColor: tier.tierColor }}>
                    <CardHeader className="pb-3">
                      <CardTitle className="text-lg flex items-center gap-2">
                        <Award className="w-5 h-5" style={{ color: tier.tierColor }} />
                        {tier.tierName}
                      </CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-2">
                      <div>
                        <Label className="text-sm font-medium">Chi tiêu tối thiểu</Label>
                        <p className="text-sm">{tier.minSpent.toLocaleString()} VND</p>
                      </div>
                      <div>
                        <Label className="text-sm font-medium">Điểm tối thiểu</Label>
                        <p className="text-sm">{tier.minPoints.toLocaleString()} điểm</p>
                      </div>
                      <div>
                        <Label className="text-sm font-medium">Hệ số điểm</Label>
                        <Badge variant="secondary">x{tier.pointsMultiplier}</Badge>
                      </div>
                      <div>
                        <Label className="text-sm font-medium">Giảm giá</Label>
                        <Badge variant="outline">{tier.discountPercentage}%</Badge>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </TabsContent>

            <TabsContent value="calculator" className="space-y-6">
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Calculator className="w-5 h-5 text-green-600" />
                    Máy tính điểm thưởng
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="flex gap-4 items-end">
                    <div className="flex-1">
                      <Label htmlFor="testAmount">Số tiền hóa đơn (VND)</Label>
                      <Input
                        id="testAmount"
                        type="number"
                        value={testAmount}
                        onChange={(e) => setTestAmount(parseInt(e.target.value) || 0)}
                        placeholder="100000"
                      />
                    </div>
                    <Button 
                      type="button" 
                      onClick={calculateTestPoints}
                      disabled={calculatePointsMutation.isPending}
                    >
                      {calculatePointsMutation.isPending ? "Đang tính..." : "Tính điểm"}
                    </Button>
                  </div>

                  {calculationResult && (
                    <Card className="bg-blue-50 border-blue-200">
                      <CardContent className="pt-6">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                          <div>
                            <Label className="text-sm font-medium">Điểm nhận được</Label>
                            <p className="text-2xl font-bold text-blue-600">
                              {calculationResult.points} điểm
                            </p>
                          </div>
                          <div>
                            <Label className="text-sm font-medium">Công thức tính</Label>
                            <p className="text-sm text-gray-600">{calculationResult.formula}</p>
                          </div>
                        </div>
                        
                        {calculationResult.bonusInfo && calculationResult.bonusInfo.length > 0 && (
                          <div className="mt-4">
                            <Label className="text-sm font-medium">Bonus áp dụng</Label>
                            <div className="flex flex-wrap gap-2 mt-2">
                              {calculationResult.bonusInfo.map((bonus: string, index: number) => (
                                <Badge key={index} variant="secondary">{bonus}</Badge>
                              ))}
                            </div>
                          </div>
                        )}
                      </CardContent>
                    </Card>
                  )}
                </CardContent>
              </Card>
            </TabsContent>

            <div className="flex justify-end pt-6 border-t">
              <Button type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? "Đang lưu..." : "Lưu cấu hình"}
              </Button>
            </div>
          </form>
        </Tabs>
      </CardContent>
    </Card>
  );
}