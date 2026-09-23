"use client";

import React, { useState } from "react";
import { useRouter } from "next/navigation";
import { Plus, Trash2, Camera, Eye, Sparkles } from "lucide-react";
import { DifficultyLevel, Ingredient, Recipe, RecipeImage, RecipeStep, RecipeStatus } from "../../lib/types";
import { Input, Textarea } from "../ui/Input";
import { Button } from "../ui/Button";
import { ImageUploadZone } from "./ImageUploadZone";
import { Modal } from "../ui/Modal";
import { Badge } from "../ui/Badge";
import { useApp } from "../../context/AppContext";

export interface RecipeFormProps {
  initialData?: Recipe;
  isEditing?: boolean;
}

export const RecipeForm: React.FC<RecipeFormProps> = ({ initialData, isEditing = false }) => {
  const router = useRouter();
  const { categories, addRecipe, updateRecipe, showToast } = useApp();

  const [title, setTitle] = useState(initialData?.title || "");
  const [description, setDescription] = useState(initialData?.description || "");
  const [selectedCategoryId, setSelectedCategoryId] = useState(
    initialData?.category?.id || (categories.length > 1 ? categories[1].id : "")
  );
  const [prepTime, setPrepTime] = useState<number>(initialData?.prepTime || 20);
  const [cookTime, setCookTime] = useState<number>(initialData?.cookTime ?? 30);
  const [servings, setServings] = useState<number>(initialData?.servings || 4);
  const [difficultyLevel, setDifficultyLevel] = useState<DifficultyLevel>(
    initialData?.difficultyLevel || "Medium"
  );

  const [images, setImages] = useState<RecipeImage[]>(initialData?.images || []);

  const [ingredients, setIngredients] = useState<Ingredient[]>(
    initialData?.ingredients || [
      { id: "ing-1", amount: "500", unit: "g", name: "Nguyên liệu chính" },
      { id: "ing-2", amount: "2", unit: "muỗng canh", name: "Gia vị nêm" },
    ]
  );

  const [steps, setSteps] = useState<RecipeStep[]>(
    initialData?.steps || [
      { id: "step-1", stepNumber: 1, description: "Sơ chế nguyên liệu sạch sẽ." },
      { id: "step-2", stepNumber: 2, description: "Bắt đầu nấu theo các bước hướng dẫn." },
    ]
  );

  const [nutrition, setNutrition] = useState({
    calories: initialData?.nutrition?.calories || 450,
    protein: initialData?.nutrition?.protein || 25,
    carbs: initialData?.nutrition?.carbs || 40,
    fat: initialData?.nutrition?.fat || 15,
  });

  const [isPreviewOpen, setIsPreviewOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Dynamic ingredient operations
  const addIngredientRow = () => {
    setIngredients([
      ...ingredients,
      { id: `ing-${Date.now()}`, amount: "", unit: "g", name: "" },
    ]);
  };

  const updateIngredientRow = (id: string, field: keyof Ingredient, value: string) => {
    setIngredients(
      ingredients.map((item) => (item.id === id ? { ...item, [field]: value } : item))
    );
  };

  const removeIngredientRow = (id: string) => {
    if (ingredients.length <= 1) {
      showToast("warning", "Công thức cần ít nhất một nguyên liệu.");
      return;
    }
    setIngredients(ingredients.filter((item) => item.id !== id));
  };

  // Dynamic steps operations
  const addStepRow = () => {
    setSteps([
      ...steps,
      {
        id: `step-${Date.now()}`,
        stepNumber: steps.length + 1,
        description: "",
      },
    ]);
  };

  const updateStepRow = (id: string, description: string) => {
    setSteps(steps.map((s) => (s.id === id ? { ...s, description } : s)));
  };

  const removeStepRow = (id: string) => {
    if (steps.length <= 1) {
      showToast("warning", "Công thức cần ít nhất một bước thực hiện.");
      return;
    }
    const filtered = steps.filter((s) => s.id !== id);
    const reordered = filtered.map((s, index) => ({
      ...s,
      stepNumber: index + 1,
    }));
    setSteps(reordered);
  };

  const validateForm = () => {
    const errs: Record<string, string> = {};
    if (!title.trim()) errs.title = "Tiêu đề công thức không được để trống";
    if (!description.trim()) errs.description = "Mô tả công thức không được để trống";
    if (prepTime <= 0) errs.prepTime = "Thời gian chuẩn bị phải lớn hơn 0";
    if (cookTime < 0) errs.cookTime = "Thời gian nấu không hợp lệ";
    if (servings <= 0) errs.servings = "Số lượng khẩu phần phải lớn hơn 0";

    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (targetStatus: RecipeStatus) => {
    if (!validateForm()) {
      showToast("error", "Vui lòng hoàn thiện các trường thông tin bắt buộc.");
      return;
    }

    setIsSubmitting(true);

    const categoryObj =
      categories.find((c) => c.id === selectedCategoryId) ||
      categories[1] ||
      categories[0];

    // Ensure at least one primary image
    const finalImages = [...images];
    if (finalImages.length > 0 && !finalImages.some((i) => i.isPrimary)) {
      finalImages[0].isPrimary = true;
    }

    const payload = {
      title,
      description,
      category: categoryObj,
      prepTime: Number(prepTime),
      cookTime: Number(cookTime),
      servings: Number(servings),
      difficultyLevel,
      status: targetStatus,
      images: finalImages,
      ingredients: ingredients.filter((ing) => ing.name.trim() !== ""),
      steps: steps.filter((st) => st.description.trim() !== ""),
      nutrition,
    };

    try {
      if (isEditing && initialData) {
        await updateRecipe(initialData.id, payload);
        router.push(`/recipes/${initialData.slug}`);
      } else {
        const created = await addRecipe(payload);
        router.push(`/recipes/${created.slug}`);
      }
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : "Không thể lưu công thức.";
      showToast("error", message);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="max-w-4xl mx-auto py-8 sm:py-12">
      <div className="mb-8">
        <h1 className="font-serif text-2xl sm:text-3xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
          {isEditing ? "Chỉnh sửa công thức" : "Tạo công thức nấu ăn mới"}
        </h1>
        <p className="text-xs sm:text-sm text-[#8A817C] dark:text-[#A8A29E] mt-1">
          Chia sẻ kinh nghiệm ẩm thực, hương vị quê hương và từng bước chế biến chuẩn vị.
        </p>
      </div>

      <div className="flex flex-col gap-8">
        {/* 1. Basic Info */}
        <section className="double-bezel">
          <div className="double-bezel-inner p-6 sm:p-8 flex flex-col gap-5">
            <div className="flex items-center gap-2 pb-3 border-b border-[#DCD8D2]/60 dark:border-[#3D3934]">
              <Sparkles className="w-4 h-4 text-[#C98F7D]" />
              <h2 className="font-serif text-lg font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                1. Thông tin cơ bản
              </h2>
            </div>

            <Input
              label="Tên món ăn / Tiêu đề công thức"
              placeholder="VD: Bún Chả Hà Nội Nướng Than Hoa"
              required
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              error={errors.title}
            />

            <Textarea
              label="Mô tả ngắn gọn hương vị và nét đặc sắc"
              placeholder="Mô tả cảm hứng, xuất xứ hoặc độ hấp dẫn của món ăn..."
              required
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              error={errors.description}
            />

            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              {/* Category */}
              <div className="flex flex-col gap-1.5">
                <label className="text-xs font-semibold text-[#463F3A] dark:text-[#EAE6DF]">
                  Danh mục món <span className="text-[#B85C5C]">*</span>
                </label>
                <select
                  value={selectedCategoryId}
                  onChange={(e) => setSelectedCategoryId(e.target.value)}
                  className="h-11 px-3 text-xs sm:text-sm rounded-xl bg-white dark:bg-[#24211E] border border-[#DCD8D2] dark:border-[#3D3934] text-[#463F3A] dark:text-[#F5F3EF] focus:border-[#C98F7D] focus:outline-none"
                >
                  {categories
                    .filter((c) => c.slug !== "all")
                    .map((cat) => (
                      <option key={cat.id} value={cat.id}>
                        {cat.name}
                      </option>
                    ))}
                </select>
              </div>

              {/* Difficulty */}
              <div className="flex flex-col gap-1.5">
                <label className="text-xs font-semibold text-[#463F3A] dark:text-[#EAE6DF]">
                  Độ khó chế biến
                </label>
                <select
                  value={difficultyLevel}
                  onChange={(e) => setDifficultyLevel(e.target.value as DifficultyLevel)}
                  className="h-11 px-3 text-xs sm:text-sm rounded-xl bg-white dark:bg-[#24211E] border border-[#DCD8D2] dark:border-[#3D3934] text-[#463F3A] dark:text-[#F5F3EF] focus:border-[#C98F7D] focus:outline-none"
                >
                  <option value="Easy">Dễ làm</option>
                  <option value="Medium">Trung bình</option>
                  <option value="Hard">Kỳ công</option>
                </select>
              </div>

              {/* Prep Time */}
              <Input
                label="Chuẩn bị (phút)"
                type="number"
                min={1}
                required
                value={prepTime}
                onChange={(e) => setPrepTime(Number(e.target.value))}
                error={errors.prepTime}
              />

              {/* Cook Time */}
              <Input
                label="Nấu chín (phút)"
                type="number"
                min={0}
                required
                value={cookTime}
                onChange={(e) => setCookTime(Number(e.target.value))}
                error={errors.cookTime}
              />
            </div>

            <div className="w-full sm:w-1/3">
              <Input
                label="Khẩu phần (người ăn)"
                type="number"
                min={1}
                value={servings}
                onChange={(e) => setServings(Number(e.target.value))}
                error={errors.servings}
              />
            </div>
          </div>
        </section>

        {/* 2. Image Upload */}
        <section className="double-bezel">
          <div className="double-bezel-inner p-6 sm:p-8 flex flex-col gap-5">
            <div className="flex items-center gap-2 pb-3 border-b border-[#DCD8D2]/60 dark:border-[#3D3934]">
              <Camera className="w-4 h-4 text-[#C98F7D]" />
              <h2 className="font-serif text-lg font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                2. Hình ảnh món ăn
              </h2>
            </div>
            <ImageUploadZone images={images} onChange={setImages} />
            {errors.images && (
              <p className="text-xs font-semibold text-[#B85C5C]">{errors.images}</p>
            )}
          </div>
        </section>

        {/* 3. Ingredients */}
        <section className="double-bezel">
          <div className="double-bezel-inner p-6 sm:p-8 flex flex-col gap-5">
            <div className="flex items-center justify-between pb-3 border-b border-[#DCD8D2]/60 dark:border-[#3D3934]">
              <h2 className="font-serif text-lg font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                3. Nguyên liệu cần chuẩn bị
              </h2>
              <Button
                type="button"
                variant="secondary"
                size="sm"
                onClick={addIngredientRow}
                leftIcon={<Plus className="w-3.5 h-3.5" />}
              >
                Thêm nguyên liệu
              </Button>
            </div>

            <div className="flex flex-col gap-2.5">
              {ingredients.map((ing, idx) => (
                <div key={ing.id} className="flex items-center gap-2 sm:gap-3">
                  <span className="text-xs font-bold text-[#8A817C] w-6 text-center">
                    {idx + 1}.
                  </span>
                  <input
                    type="text"
                    value={ing.amount}
                    onChange={(e) => updateIngredientRow(ing.id, "amount", e.target.value)}
                    placeholder="Định lượng (500)"
                    className="w-24 sm:w-28 h-10 px-3 text-xs rounded-xl bg-white dark:bg-[#24211E] border border-[#DCD8D2] dark:border-[#3D3934] focus:border-[#C98F7D] focus:outline-none"
                  />
                  <select
                    value={ing.unit}
                    onChange={(e) => updateIngredientRow(ing.id, "unit", e.target.value)}
                    className="w-24 sm:w-28 h-10 px-2 text-xs rounded-xl bg-white dark:bg-[#24211E] border border-[#DCD8D2] dark:border-[#3D3934] focus:border-[#C98F7D] focus:outline-none"
                  >
                    <option value="g">gram (g)</option>
                    <option value="kg">kilogram</option>
                    <option value="ml">ml</option>
                    <option value="l">lít</option>
                    <option value="muỗng canh">muỗng canh</option>
                    <option value="muỗng cà phê">muỗng cà phê</option>
                    <option value="quả">quả / trái</option>
                    <option value="củ">củ</option>
                    <option value="nhánh">nhánh</option>
                    <option value="bó">bó</option>
                    <option value="miếng">miếng</option>
                    <option value="tép">tép</option>
                  </select>
                  <input
                    type="text"
                    value={ing.name}
                    onChange={(e) => updateIngredientRow(ing.id, "name", e.target.value)}
                    placeholder="Tên nguyên liệu (VD: Thịt ba chỉ, Nước mắm...)"
                    className="flex-1 h-10 px-3 text-xs rounded-xl bg-white dark:bg-[#24211E] border border-[#DCD8D2] dark:border-[#3D3934] focus:border-[#C98F7D] focus:outline-none"
                  />
                  <button
                    type="button"
                    onClick={() => removeIngredientRow(ing.id)}
                    aria-label="Xóa nguyên liệu"
                    className="p-2 text-[#8A817C] hover:text-[#B85C5C] hover:bg-[#F9EBEB] rounded-lg transition-colors cursor-pointer"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* 4. Steps */}
        <section className="double-bezel">
          <div className="double-bezel-inner p-6 sm:p-8 flex flex-col gap-5">
            <div className="flex items-center justify-between pb-3 border-b border-[#DCD8D2]/60 dark:border-[#3D3934]">
              <h2 className="font-serif text-lg font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                4. Các bước thực hiện chi tiết
              </h2>
              <Button
                type="button"
                variant="secondary"
                size="sm"
                onClick={addStepRow}
                leftIcon={<Plus className="w-3.5 h-3.5" />}
              >
                Thêm bước
              </Button>
            </div>

            <div className="flex flex-col gap-4">
              {steps.map((step) => (
                <div
                  key={step.id}
                  className="p-4 rounded-xl border border-[#DCD8D2]/80 dark:border-[#3D3934] bg-[#FAF9F6] dark:bg-[#201D1B] flex flex-col gap-3"
                >
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-bold text-[#C98F7D] uppercase tracking-wider">
                      Bước {step.stepNumber}
                    </span>
                    <button
                      type="button"
                      onClick={() => removeStepRow(step.id)}
                      className="text-xs text-[#8A817C] hover:text-[#B85C5C] flex items-center gap-1 cursor-pointer"
                    >
                      <Trash2 className="w-3.5 h-3.5" />
                      <span>Xóa bước</span>
                    </button>
                  </div>

                  <Textarea
                    placeholder={`Mô tả chi tiết cách xử lý ở bước ${step.stepNumber}...`}
                    rows={2}
                    value={step.description}
                    onChange={(e) => updateStepRow(step.id, e.target.value)}
                  />
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* 5. Nutrition (Optional) */}
        <section className="double-bezel">
          <div className="double-bezel-inner p-6 sm:p-8 flex flex-col gap-5">
            <div className="pb-3 border-b border-[#DCD8D2]/60 dark:border-[#3D3934]">
              <h2 className="font-serif text-lg font-bold text-[#463F3A] dark:text-[#F5F3EF]">
                5. Thông tin dinh dưỡng (ước tính trên 1 phần ăn)
              </h2>
              <p className="text-xs text-[#8A817C]">Tùy chọn, giúp người đọc nắm bắt hàm lượng dưỡng chất.</p>
            </div>

            <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
              <Input
                label="Calories (kcal)"
                type="number"
                value={nutrition.calories}
                onChange={(e) => setNutrition({ ...nutrition, calories: Number(e.target.value) })}
              />
              <Input
                label="Chất đạm / Protein (g)"
                type="number"
                value={nutrition.protein}
                onChange={(e) => setNutrition({ ...nutrition, protein: Number(e.target.value) })}
              />
              <Input
                label="Tinh bột / Carbs (g)"
                type="number"
                value={nutrition.carbs}
                onChange={(e) => setNutrition({ ...nutrition, carbs: Number(e.target.value) })}
              />
              <Input
                label="Chất béo / Fat (g)"
                type="number"
                value={nutrition.fat}
                onChange={(e) => setNutrition({ ...nutrition, fat: Number(e.target.value) })}
              />
            </div>
          </div>
        </section>

        {/* Form Action Buttons */}
        <div className="sticky bottom-4 z-30 p-4 rounded-2xl bg-white/95 dark:bg-[#24211E]/95 backdrop-blur-md border border-[#DCD8D2] dark:border-[#3D3934] shadow-xl flex items-center justify-between gap-3">
          <Button
            type="button"
            variant="outline"
            size="md"
            onClick={() => setIsPreviewOpen(true)}
            leftIcon={<Eye className="w-4 h-4" />}
          >
            Xem trước
          </Button>

          <div className="flex items-center gap-3">
            <Button
              type="button"
              variant="secondary"
              size="md"
              isLoading={isSubmitting}
              onClick={() => handleSubmit("Draft")}
            >
              Lưu bản nháp
            </Button>
            <Button
              type="button"
              variant="primary"
              size="md"
              isLoading={isSubmitting}
              onClick={() => handleSubmit("Published")}
            >
              {isEditing ? "Cập nhật & Xuất bản" : "Xuất bản công thức"}
            </Button>
          </div>
        </div>
      </div>

      {/* Preview Modal */}
      <Modal
        isOpen={isPreviewOpen}
        onClose={() => setIsPreviewOpen(false)}
        title="Xem trước công thức"
        size="lg"
        footer={
          <Button variant="primary" size="sm" onClick={() => setIsPreviewOpen(false)}>
            Đóng xem trước
          </Button>
        }
      >
        <div className="flex flex-col gap-4">
          <div className="flex items-center gap-2">
            <Badge variant="category">
              {categories.find((c) => c.id === selectedCategoryId)?.name || "Món ngon"}
            </Badge>
            <Badge variant="difficulty" difficulty={difficultyLevel} />
            <Badge variant="time">{Number(prepTime) + Number(cookTime)} phút</Badge>
          </div>
          <h2 className="font-serif text-2xl font-bold text-[#463F3A] dark:text-[#F5F3EF]">
            {title || "Tiêu đề công thức"}
          </h2>
          <p className="text-xs text-[#8A817C] leading-relaxed">
            {description || "Chưa có mô tả..."}
          </p>

          <div className="mt-2 border-t pt-3">
            <h4 className="font-serif font-bold text-sm mb-2">Nguyên liệu ({ingredients.length})</h4>
            <ul className="list-disc pl-5 text-xs flex flex-col gap-1 text-[#463F3A]">
              {ingredients.map((ing, i) => (
                <li key={i}>
                  {ing.amount} {ing.unit} {ing.name}
                </li>
              ))}
            </ul>
          </div>
        </div>
      </Modal>
    </div>
  );
};
